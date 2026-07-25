using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.BLL.Mapping;
using QuizArena.BLL.Notifications;
using QuizArena.BLL.Scoring;
using QuizArena.BLL.Validation;
using QuizArena.Core.Aspects.Transaction;
using QuizArena.Core.Aspects.Validation;
using QuizArena.Core.DataAccess.Paging;
using QuizArena.Core.Exceptions;
using QuizArena.Core.Utilities.Clock;
using QuizArena.Core.Utilities.Results;
using QuizArena.Core.Utilities.Security;
using QuizArena.DAL.Abstract;
using QuizArena.DAL.ReadModels;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Dtos.Common;
using QuizArena.Entities.Dtos.Play;
using QuizArena.Entities.Dtos.Statistics;
using QuizArena.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace QuizArena.BLL.Concrete;

/// <summary>
/// Yarışma akışının motoru: soruyu sunar, cevabı doğrular, puanlar, bitirir.
/// </summary>
/// <remarks>
/// <para><b>Hile önlemenin üç ayağı:</b></para>
/// <list type="number">
///   <item>
///     <b>Doğru cevap istemciye gitmez.</b> Soru
///     <see cref="QuizQuestionResponse"/> ile sunulur; o tipte doğruluk alanı
///     yoktur. Cevap ancak gönderildikten sonra açıklanır.
///   </item>
///   <item>
///     <b>Süreyi sunucu ölçer.</b> Sorunun sunulduğu an
///     (<c>AskedAtUtc</c>) sunucu tarafından yazılır. İstemci "kaç saniyede
///     cevapladım" bilgisi göndermez; gönderse de kullanılmaz.
///   </item>
///   <item>
///     <b>Aynı soruya ikinci cevap kabul edilmez.</b> Hem kod kontrol eder hem
///     de <c>CompetitionAnswers.CompetitionQuestionId</c> üzerindeki tekil
///     indeks veritabanı seviyesinde engeller. İki isteğin aynı anda gelmesi
///     (race condition) durumunda bile puan iki kez yazılamaz.
///   </item>
/// </list>
/// </remarks>
public sealed class GameManager : IGameService
{
    private readonly ICompetitionRepository _competitionRepository;
    private readonly ICompetitionQuestionRepository _competitionQuestionRepository;
    private readonly ICompetitionAnswerRepository _competitionAnswerRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomParticipantRepository _participantRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IStatisticService _statisticService;
    private readonly IAchievementService _achievementService;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameNotifier _notifier;
    private readonly IClock _clock;
    private readonly ILogger<GameManager> _logger;

    public GameManager(
        ICompetitionRepository competitionRepository,
        ICompetitionQuestionRepository competitionQuestionRepository,
        ICompetitionAnswerRepository competitionAnswerRepository,
        IRoomRepository roomRepository,
        IRoomParticipantRepository participantRepository,
        IQuestionRepository questionRepository,
        IStatisticService statisticService,
        IAchievementService achievementService,
        ICurrentUserService currentUser,
        IGameNotifier notifier,
        IClock clock,
        ILogger<GameManager> logger)
    {
        _competitionRepository = competitionRepository;
        _competitionQuestionRepository = competitionQuestionRepository;
        _competitionAnswerRepository = competitionAnswerRepository;
        _roomRepository = roomRepository;
        _participantRepository = participantRepository;
        _questionRepository = questionRepository;
        _statisticService = statisticService;
        _achievementService = achievementService;
        _currentUser = currentUser;
        _notifier = notifier;
        _clock = clock;
        _logger = logger;
    }

    // =====================================================================
    //  Sıradaki soruyu sun
    // =====================================================================
    [TransactionAspect]
    public async Task<IDataResult<QuizQuestionResponse?>> GetCurrentQuestionAsync(
        CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();

        Competition competition = await _competitionRepository.GetActiveForUserAsync(
                                      userId, asNoTracking: false, cancellationToken)
                                  ?? throw new BusinessException(Messages.NoActiveCompetition);

        int secondsPerQuestion = competition.Room.SecondsPerQuestion;

        // Döngünün üst sınırı soru sayısı + 1: süresi geçmiş sorular
        // kapatılırken sonsuz döngüye girme olasılığı bırakılmıyor.
        for (var guard = 0; guard <= competition.QuestionCount; guard++)
        {
            CompetitionQuestion? next = await _competitionQuestionRepository.GetNextUnansweredAsync(
                competition.Id, asNoTracking: false, cancellationToken);

            if (next is null)
            {
                // Cevaplanmamış soru kalmadı → yarışma bitti.
                await FinishCompetitionAsync(competition, cancellationToken);
                return new SuccessDataResult<QuizQuestionResponse?>(null, Messages.CompetitionCompleted);
            }

            DateTime now = _clock.UtcNow;

            // İlk kez sunuluyor: zaman damgalarını SUNUCU yazar.
            if (next.AskedAtUtc is null)
            {
                next.AskedAtUtc = now;
                next.ClosesAtUtc = now.AddSeconds(secondsPerQuestion);
                await _competitionQuestionRepository.UpdateAsync(next, cancellationToken);

                return new SuccessDataResult<QuizQuestionResponse?>(
                    next.ToQuizResponse(
                        competition.QuestionCount,
                        competition.TotalScore,
                        competition.CurrentStreak,
                        next.ClosesAtUtc.Value));
            }

            DateTime closesAt = next.ClosesAtUtc ?? next.AskedAtUtc.Value.AddSeconds(secondsPerQuestion);

            // Süre henüz dolmamış: aynı soru, kalan süresiyle tekrar sunulur.
            // (Sayfa yenilendiğinde oyuncu soruyu kaybetmez.)
            if (now <= closesAt.AddMilliseconds(GameRules.TimingToleranceMilliseconds))
            {
                return new SuccessDataResult<QuizQuestionResponse?>(
                    next.ToQuizResponse(
                        competition.QuestionCount,
                        competition.TotalScore,
                        competition.CurrentStreak,
                        closesAt));
            }

            // Süre dolmuş: soruyu "cevaplanmadı" olarak kapat ve sıradakine geç.
            // Oyuncu sekmeyi kapatıp döndüğünde geçmiş sorular boşta kalmaz.
            await RecordTimeoutAsync(competition, next, closesAt, cancellationToken);
        }

        // Buraya düşmek beklenmeyen bir durumdur; yarışmayı kapatıp
        // kullanıcıyı sonuç ekranına yönlendiriyoruz.
        _logger.LogWarning(
            "Soru sunma döngüsü beklenmedik şekilde sınıra ulaştı. Yarışma: {CompetitionId}",
            competition.Id);

        await FinishCompetitionAsync(competition, cancellationToken);
        return new SuccessDataResult<QuizQuestionResponse?>(null, Messages.CompetitionCompleted);
    }

    // =====================================================================
    //  Cevap gönder
    // =====================================================================
    [ValidationAspect(typeof(SubmitAnswerRequestValidator))]
    [TransactionAspect]
    public async Task<IDataResult<AnswerResultResponse>> SubmitAnswerAsync(
        SubmitAnswerRequest request,
        CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();

        CompetitionQuestion competitionQuestion =
            await _competitionQuestionRepository.GetForAnsweringAsync(request.CompetitionQuestionId, cancellationToken)
            ?? throw new NotFoundException(Messages.QuestionNotFound);

        Competition competition = competitionQuestion.Competition;

        // --- Sahiplik kontrolü ----------------------------------------------
        // Bu kontrol olmadan, geçerli bir jetona sahip herhangi bir kullanıcı
        // başka birinin yarışma sorusuna cevap gönderip onun skorunu
        // değiştirebilirdi (IDOR — Insecure Direct Object Reference).
        if (competition.RoomParticipant.UserId != userId)
        {
            throw new ForbiddenException();
        }

        if (competition.Status != CompetitionStatus.InProgress)
        {
            throw new BusinessException(Messages.CompetitionAlreadyFinished);
        }

        if (competitionQuestion.Answer is not null)
        {
            throw new ConflictException(Messages.QuestionAlreadyAnswered);
        }

        if (competitionQuestion.AskedAtUtc is null)
        {
            // Soru hiç sunulmadan cevap gelmesi, istemcinin akışı atlamaya
            // çalıştığını gösterir (ör. soru kimliklerini tahmin ederek
            // hepsini toptan cevaplamak).
            throw new BusinessException(Messages.QuestionNotServedYet);
        }

        Room room = await _roomRepository.GetAsync(r => r.Id == competition.RoomId,
                        cancellationToken: cancellationToken)
                    ?? throw new NotFoundException(Messages.RoomNotFound);

        DateTime now = _clock.UtcNow;
        DateTime askedAt = competitionQuestion.AskedAtUtc.Value;
        DateTime closesAt = competitionQuestion.ClosesAtUtc ?? askedAt.AddSeconds(room.SecondsPerQuestion);

        // Süre SUNUCUDA ölçülür. Negatif değer (saat kayması) sıfıra çekilir.
        int elapsedMilliseconds = Math.Max(0, (int)(now - askedAt).TotalMilliseconds);

        bool isTimedOut = now > closesAt.AddMilliseconds(GameRules.TimingToleranceMilliseconds);

        // --- Seçilen şık gerçekten bu soruya mı ait? -------------------------
        Answer? selectedAnswer = null;
        if (request.SelectedAnswerId is not null)
        {
            selectedAnswer = competitionQuestion.Question.Answers
                .FirstOrDefault(a => a.Id == request.SelectedAnswerId.Value);

            // Başka bir sorunun şıkkını göndermek, cevap kimliklerini
            // deneyerek doğru cevabı arama yöntemidir; reddediyoruz.
            if (selectedAnswer is null)
            {
                throw new BusinessException(Messages.AnswerDoesNotBelongToQuestion);
            }
        }

        Answer correctAnswer = competitionQuestion.Question.Answers.FirstOrDefault(a => a.IsCorrect)
                               ?? throw new BusinessException(Messages.QuestionNeedsOneCorrectAnswer);

        bool isCorrect = !isTimedOut && selectedAnswer is not null && selectedAnswer.IsCorrect;

        ScoreBreakdown score = ScoreCalculator.Calculate(
            competitionQuestion.Question.Difficulty,
            isCorrect,
            elapsedMilliseconds,
            room.SecondsPerQuestion,
            competition.CurrentStreak);

        // --- Cevabı kaydet ---------------------------------------------------
        var answerRecord = new CompetitionAnswer
        {
            CompetitionQuestionId = competitionQuestion.Id,
            SelectedAnswerId = selectedAnswer?.Id,
            AnsweredAtUtc = now,
            ElapsedMilliseconds = elapsedMilliseconds,
            IsCorrect = isCorrect,
            IsTimedOut = isTimedOut,
            BasePoints = score.BasePoints,
            SpeedBonus = score.SpeedBonus,
            StreakBonus = score.StreakBonus,
            EarnedPoints = score.Total
        };

        await _competitionAnswerRepository.AddAsync(answerRecord, cancellationToken);

        // --- Yarışma sayaçlarını güncelle ------------------------------------
        ApplyAnswerToCompetition(competition, isCorrect, isTimedOut, score.Total);
        await _competitionRepository.UpdateAsync(competition, cancellationToken);

        // Katılımcının odadaki görünen skoru da güncellenir (skor tablosu bunu okur).
        RoomParticipant? participant = await _participantRepository.GetAsync(
            p => p.Id == competition.RoomParticipantId, asNoTracking: false, cancellationToken: cancellationToken);

        if (participant is not null)
        {
            participant.TotalScore = competition.TotalScore;
            participant.LastSeenAtUtc = now;
            await _participantRepository.UpdateAsync(participant, cancellationToken);
        }

        if (isCorrect)
        {
            // Sorunun gerçek zorluk oranını ölçmek için (kaç kez doğru bilindi).
            await _questionRepository.IncrementCorrectStatisticAsync(
                competitionQuestion.QuestionId, cancellationToken);
        }

        // --- Yarışma bitti mi? -----------------------------------------------
        bool hasNextQuestion = competition.AnsweredCount < competition.QuestionCount;

        if (!hasNextQuestion)
        {
            await FinishCompetitionAsync(competition, cancellationToken);
        }

        await PublishScoreboardAsync(competition.RoomId, isFinal: !hasNextQuestion, cancellationToken);

        var response = new AnswerResultResponse(
            competition.Id,
            isCorrect,
            isTimedOut,
            correctAnswer.Id,
            correctAnswer.Text,
            competitionQuestion.Question.Explanation,
            elapsedMilliseconds,
            score,
            competition.TotalScore,
            competition.CurrentStreak,
            competition.AnsweredCount,
            competition.QuestionCount,
            hasNextQuestion);

        return new SuccessDataResult<AnswerResultResponse>(response, Messages.AnswerAccepted);
    }

    // =====================================================================
    //  Sonuç ekranı
    // =====================================================================
    public async Task<IDataResult<CompetitionSummaryResponse>> GetSummaryAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();

        Competition competition = await _competitionRepository.GetAsync(
                                      c => c.Id == competitionId,
                                      include: q => q
                                          .Include(c => c.Room).ThenInclude(r => r.Category)
                                          .Include(c => c.RoomParticipant),
                                      cancellationToken: cancellationToken)
                                  ?? throw new NotFoundException(Messages.CompetitionNotFound);

        // Başkasının yarışma sonucu görüntülenemez.
        if (competition.RoomParticipant.UserId != userId)
        {
            throw new ForbiddenException();
        }

        IReadOnlyList<ScoreboardRow> rows =
            await _competitionRepository.GetScoreboardAsync(competition.RoomId, cancellationToken);

        IReadOnlyList<ScoreboardEntryResponse> scoreboard = ToScoreboard(rows);

        IReadOnlyList<AchievementResponse> achievements =
            await _achievementService.GetEarnedInCompetitionAsync(competitionId, cancellationToken);

        int? rank = competition.Room.IsMultiplayer
            ? scoreboard.FirstOrDefault(e => e.UserId == userId)?.Rank
            : null;

        var summary = new CompetitionSummaryResponse(
            competition.Id,
            competition.RoomId,
            competition.Room.Category?.Name ?? string.Empty,
            competition.TotalScore,
            competition.QuestionCount,
            competition.CorrectCount,
            competition.WrongCount,
            competition.TimedOutCount,
            competition.AccuracyPercentage,
            competition.LongestStreak,
            competition.CalculateDurationSeconds(),
            rank,
            scoreboard,
            achievements);

        return new SuccessDataResult<CompetitionSummaryResponse>(summary);
    }

    // =====================================================================
    //  Yarışmayı yarıda bırak
    // =====================================================================
    [TransactionAspect]
    public async Task<IResult> AbandonAsync(CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();

        Competition competition = await _competitionRepository.GetActiveForUserAsync(
                                      userId, asNoTracking: false, cancellationToken)
                                  ?? throw new BusinessException(Messages.NoActiveCompetition);

        competition.Status = CompetitionStatus.Abandoned;
        competition.FinishedAtUtc = _clock.UtcNow;
        await _competitionRepository.UpdateAsync(competition, cancellationToken);

        // Yarıda bırakılan yarışma istatistiklere işlenmez: aksi hâlde
        // "kötü gidiyor" diyerek yarışmayı bırakmak, doğruluk oranını
        // korumanın bir yolu olurdu.
        await CloseRoomIfAllCompetitionsDoneAsync(competition.RoomId, cancellationToken);

        return new SuccessResult(Messages.CompetitionAbandoned);
    }

    // =====================================================================
    //  Geçmiş
    // =====================================================================
    public async Task<IDataResult<PagedResponse<CompetitionHistoryResponse>>> GetMyHistoryAsync(
        PageRequest pageRequest,
        CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();

        PagedList<Competition> page =
            await _competitionRepository.GetHistoryForUserAsync(userId, pageRequest, cancellationToken);

        PagedList<CompetitionHistoryResponse> mapped = page.Map(c => c.ToHistoryResponse());

        return new SuccessDataResult<PagedResponse<CompetitionHistoryResponse>>(
            PagedResponse<CompetitionHistoryResponse>.From(mapped));
    }

    // =====================================================================
    //  İç akış
    // =====================================================================

    /// <summary>Süresi dolan soruyu "cevaplanmadı" olarak kapatır.</summary>
    private async Task RecordTimeoutAsync(
        Competition competition,
        CompetitionQuestion competitionQuestion,
        DateTime closesAt,
        CancellationToken cancellationToken)
    {
        var timeoutAnswer = new CompetitionAnswer
        {
            CompetitionQuestionId = competitionQuestion.Id,
            SelectedAnswerId = null,
            AnsweredAtUtc = closesAt,
            ElapsedMilliseconds = (int)(closesAt - competitionQuestion.AskedAtUtc!.Value).TotalMilliseconds,
            IsCorrect = false,
            IsTimedOut = true,
            BasePoints = 0,
            SpeedBonus = 0,
            StreakBonus = 0,
            EarnedPoints = 0
        };

        await _competitionAnswerRepository.AddAsync(timeoutAnswer, cancellationToken);

        ApplyAnswerToCompetition(competition, isCorrect: false, isTimedOut: true, earnedPoints: 0);
        await _competitionRepository.UpdateAsync(competition, cancellationToken);
    }

    /// <summary>
    /// Cevabın yarışma sayaçlarına etkisi. Tek yerde tutulması, "süresi dolan
    /// soru" ile "gönderilen cevap" yollarının aynı kuralı uygulamasını garanti eder.
    /// </summary>
    private static void ApplyAnswerToCompetition(
        Competition competition,
        bool isCorrect,
        bool isTimedOut,
        int earnedPoints)
    {
        if (isCorrect)
        {
            competition.CorrectCount++;
            competition.CurrentStreak++;
            competition.LongestStreak = Math.Max(competition.LongestStreak, competition.CurrentStreak);
            competition.TotalScore += earnedPoints;
            return;
        }

        // Seri her yanlışta sıfırlanır.
        competition.CurrentStreak = 0;

        if (isTimedOut)
        {
            competition.TimedOutCount++;
        }
        else
        {
            competition.WrongCount++;
        }
    }

    /// <summary>
    /// Yarışmayı tamamlar: istatistikleri işler, rozetleri değerlendirir,
    /// gerekirse odayı kapatır.
    /// </summary>
    private async Task FinishCompetitionAsync(Competition competition, CancellationToken cancellationToken)
    {
        if (competition.Status != CompetitionStatus.InProgress)
        {
            return;
        }

        competition.Status = CompetitionStatus.Completed;
        competition.FinishedAtUtc = _clock.UtcNow;
        await _competitionRepository.UpdateAsync(competition, cancellationToken);

        // Cevap süresi ortalaması ve "hızlı doğru cevap var mı" bilgisi için
        // bu yarışmanın cevapları tek sorguda çekilir.
        IReadOnlyList<CompetitionAnswer> answers = await _competitionAnswerRepository.GetListAsync(
            ca => ca.CompetitionQuestion.CompetitionId == competition.Id,
            cancellationToken: cancellationToken);

        // Süresi dolan sorular ortalamayı bozar (her zaman tam süre görünür),
        // bu yüzden ortalama yalnızca gerçekten cevaplananlar üzerinden alınır.
        CompetitionAnswer[] answered = answers.Where(a => !a.IsTimedOut).ToArray();
        int averageMilliseconds = answered.Length == 0
            ? 0
            : (int)answered.Average(a => a.ElapsedMilliseconds);

        bool hasFastCorrectAnswer = answers.Any(a =>
            a.IsCorrect && a.ElapsedMilliseconds < GameRules.QuickThinkerThresholdMilliseconds);

        Guid userId = competition.RoomParticipant?.UserId
                      ?? (await _participantRepository.GetAsync(
                              p => p.Id == competition.RoomParticipantId,
                              cancellationToken: cancellationToken))?.UserId
                      ?? throw new BusinessException(Messages.NotInRoom);

        UserStatistic statistic = await _statisticService.ApplyCompetitionResultAsync(
            competition, userId, averageMilliseconds, cancellationToken);

        await _achievementService.EvaluateAsync(
            competition, userId, statistic, hasFastCorrectAnswer, cancellationToken);

        _logger.LogInformation(
            "Yarışma tamamlandı: {CompetitionId} — {Score} puan, {Correct}/{Total} doğru.",
            competition.Id,
            competition.TotalScore,
            competition.CorrectCount,
            competition.QuestionCount);

        await CloseRoomIfAllCompetitionsDoneAsync(competition.RoomId, cancellationToken);
    }

    /// <summary>
    /// Odadaki tüm yarışmalar sonuçlandıysa odayı kapatır ve çok oyunculu
    /// odada birinciye galibiyet + şampiyonluk rozeti verir.
    /// </summary>
    private async Task CloseRoomIfAllCompetitionsDoneAsync(Guid roomId, CancellationToken cancellationToken)
    {
        int stillRunning = await _competitionRepository.CountByRoomAndStatusAsync(
            roomId, CompetitionStatus.InProgress, cancellationToken);

        if (stillRunning > 0)
        {
            return;
        }

        Room? room = await _roomRepository.GetAsync(r => r.Id == roomId, asNoTracking: false,
            cancellationToken: cancellationToken);

        if (room is null || room.Status != RoomStatus.InProgress)
        {
            return;
        }

        room.Status = RoomStatus.Finished;
        room.FinishedAtUtc = _clock.UtcNow;
        await _roomRepository.UpdateAsync(room, cancellationToken);

        IReadOnlyList<ScoreboardRow> rows = await _competitionRepository.GetScoreboardAsync(roomId, cancellationToken);

        // Birincilik yalnızca çok oyunculu odada ve en az iki oyuncu
        // tamamladığında anlamlıdır: tek kişilik yarışmada "kazanmak" yok.
        ScoreboardRow[] finished = rows.Where(r => r.IsFinished).ToArray();

        if (room.IsMultiplayer && finished.Length >= 2)
        {
            ScoreboardRow winner = finished[0]; // sorgu puana göre sıralı gelir

            await _statisticService.RegisterWinAsync(winner.UserId, cancellationToken);

            Competition? winnerCompetition = await _competitionRepository.GetAsync(
                c => c.RoomId == roomId && c.RoomParticipant.UserId == winner.UserId,
                cancellationToken: cancellationToken);

            await _achievementService.GrantAsync(
                winner.UserId, AchievementCode.Champion, winnerCompetition?.Id, cancellationToken);
        }

        await _notifier.GameFinishedAsync(roomId, ToScoreboard(rows), cancellationToken);
    }

    private async Task PublishScoreboardAsync(Guid roomId, bool isFinal, CancellationToken cancellationToken)
    {
        // Nihai skor tablosu CloseRoomIfAllCompetitionsDoneAsync tarafından
        // ayrıca yayınlanıyor; burada tekrar göndermiyoruz.
        if (isFinal)
        {
            return;
        }

        IReadOnlyList<ScoreboardRow> rows = await _competitionRepository.GetScoreboardAsync(roomId, cancellationToken);
        await _notifier.ScoreboardUpdatedAsync(roomId, ToScoreboard(rows), cancellationToken);
    }

    private static ScoreboardEntryResponse[] ToScoreboard(IReadOnlyList<ScoreboardRow> rows)
        => rows.Select((row, index) => row.ToResponse(index + 1)).ToArray();
}
