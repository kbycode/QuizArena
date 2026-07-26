using System.Security.Cryptography;
using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.BLL.Mapping;
using QuizArena.BLL.Notifications;
using QuizArena.BLL.Utilities;
using QuizArena.BLL.Validation;
using QuizArena.Core.Aspects.Transaction;
using QuizArena.Core.Aspects.Validation;
using QuizArena.Core.DataAccess.Paging;
using QuizArena.Core.Exceptions;
using QuizArena.Core.Utilities.Clock;
using QuizArena.Core.Utilities.Results;
using QuizArena.Core.Utilities.Security;
using QuizArena.DAL.Abstract;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Dtos.Common;
using QuizArena.Entities.Dtos.Rooms;
using QuizArena.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace QuizArena.BLL.Concrete;

/// <summary>
/// Oda yaşam döngüsü ve yarışma başlatma.
/// </summary>
/// <remarks>
/// Oda ve yarışma jenerik CRUD uçlarıyla yönetilemez: "odaya kim
/// katılabilir", "yarışmayı kim başlatabilir", "sorular kime hangi sırayla
/// gider" sorularının yanıtı bir tabloda değil, iş kuralında saklıdır.
/// Bu servis <see cref="GameManager"/> ile birlikte o kuralları taşır.
/// </remarks>
public sealed class RoomManager : IRoomService
{
    /// <summary>Katılım kodu çakışırsa kaç kez yeniden denenir.</summary>
    private const int JoinCodeMaxAttempts = 10;

    private readonly IRoomRepository _roomRepository;
    private readonly IRoomParticipantRepository _participantRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly ICompetitionRepository _competitionRepository;
    private readonly ICompetitionQuestionRepository _competitionQuestionRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameNotifier _notifier;
    private readonly IClock _clock;
    private readonly ILogger<RoomManager> _logger;

    public RoomManager(
        IRoomRepository roomRepository,
        IRoomParticipantRepository participantRepository,
        ICategoryRepository categoryRepository,
        IQuestionRepository questionRepository,
        ICompetitionRepository competitionRepository,
        ICompetitionQuestionRepository competitionQuestionRepository,
        ICurrentUserService currentUser,
        IGameNotifier notifier,
        IClock clock,
        ILogger<RoomManager> logger)
    {
        _roomRepository = roomRepository;
        _participantRepository = participantRepository;
        _categoryRepository = categoryRepository;
        _questionRepository = questionRepository;
        _competitionRepository = competitionRepository;
        _competitionQuestionRepository = competitionQuestionRepository;
        _currentUser = currentUser;
        _notifier = notifier;
        _clock = clock;
        _logger = logger;
    }

    // =====================================================================
    //  Oda kurma
    // =====================================================================
    [ValidationAspect(typeof(CreateRoomRequestValidator))]
    [TransactionAspect]
    public async Task<IDataResult<RoomResponse>> CreateAsync(
        CreateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();

        Category category = await _categoryRepository.GetAsync(
                                c => c.Id == request.CategoryId, cancellationToken: cancellationToken)
                            ?? throw new NotFoundException(Messages.CategoryNotFound);

        if (!category.IsActive)
        {
            throw new BusinessException(Messages.CategoryInactive);
        }

        // Kategoride yeterli soru yoksa oda kurmanın anlamı yok; hatayı
        // yarışma başlangıcında değil BURADA veriyoruz ki kullanıcı arkadaşlarını
        // davet ettikten sonra hüsrana uğramasın.
        int availableQuestions = (await _questionRepository
            .GetSelectableQuestionIdsAsync(request.CategoryId, cancellationToken)).Count;

        if (availableQuestions < request.QuestionCount)
        {
            throw new BusinessException(
                $"{Messages.NotEnoughQuestions} (Bu kategoride {availableQuestions} uygun soru var.)");
        }

        // Aynı kullanıcının paralel odalarda olması skor/istatistik tutarlılığını
        // bozar ve arayüzde "hangi odadayım?" belirsizliği yaratır.
        Room? existingRoom = await _roomRepository.GetActiveRoomForUserAsync(userId, _clock.UtcNow, cancellationToken);
        if (existingRoom is not null)
        {
            throw new ConflictException(Messages.AlreadyInAnotherRoom);
        }

        var room = new Room
        {
            CategoryId = category.Id,
            HostUserId = userId,
            Name = string.IsNullOrWhiteSpace(request.Name)
                ? $"{category.Name} Odası"
                : request.Name.Trim(),
            JoinCode = await GenerateUniqueJoinCodeAsync(cancellationToken),
            Mode = request.Mode,
            Status = RoomStatus.Waiting,
            QuestionCount = request.QuestionCount,
            SecondsPerQuestion = request.SecondsPerQuestion,
            MaxPlayers = request.Mode == RoomMode.Solo ? 1 : request.MaxPlayers
        };

        await _roomRepository.AddAsync(room, cancellationToken);

        await _participantRepository.AddAsync(
            new RoomParticipant
            {
                RoomId = room.Id,
                UserId = userId,
                Role = ParticipantRole.Host,
                JoinOrder = 1,
                IsReady = true,
                LastSeenAtUtc = _clock.UtcNow
            },
            cancellationToken);

        // Tek kişilik odada beklenecek kimse yok: yarışmayı hemen başlatıyoruz
        // ki oyuncu gereksiz bir "başlat" adımına zorlanmasın.
        if (room.Mode == RoomMode.Solo)
        {
            await StartInternalAsync(room.Id, userId, cancellationToken);
        }

        Room detail = await LoadDetailAsync(room.Id, cancellationToken);

        _logger.LogInformation("Oda kuruldu: {RoomId} ({Mode})", room.Id, room.Mode);

        return new SuccessDataResult<RoomResponse>(
            detail.ToResponse(includeJoinCode: true),
            Messages.RoomCreated);
    }

    // =====================================================================
    //  Odaya katılma
    // =====================================================================
    [ValidationAspect(typeof(JoinRoomRequestValidator))]
    [TransactionAspect]
    public async Task<IDataResult<RoomResponse>> JoinAsync(
        JoinRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();
        string joinCode = request.JoinCode.Trim().ToUpperInvariant();

        Room room = await _roomRepository.GetByJoinCodeAsync(joinCode, asNoTracking: false,
                        cancellationToken: cancellationToken)
                    ?? throw new NotFoundException(Messages.RoomNotFound);

        if (room.Mode == RoomMode.Solo)
        {
            throw new BusinessException(Messages.SoloRoomCannotBeJoined);
        }

        if (room.Status != RoomStatus.Waiting)
        {
            throw new BusinessException(
                room.Status == RoomStatus.InProgress ? Messages.RoomAlreadyStarted : Messages.RoomNotWaiting);
        }

        RoomParticipant? existing = await _participantRepository.GetByRoomAndUserAsync(
            room.Id, userId, cancellationToken: cancellationToken);

        if (existing is not null)
        {
            // Katılım isteğinin tekrarı hata değil: kullanıcı sayfayı
            // yenilemiş olabilir. Mevcut oda durumunu döndürüyoruz.
            Room current = await LoadDetailAsync(room.Id, cancellationToken);
            return new SuccessDataResult<RoomResponse>(
                current.ToResponse(includeJoinCode: true),
                Messages.AlreadyInRoom);
        }

        int participantCount = await _participantRepository.CountByRoomAsync(room.Id, cancellationToken);
        if (participantCount >= room.MaxPlayers)
        {
            throw new ConflictException(Messages.RoomFull);
        }

        Room? otherRoom = await _roomRepository.GetActiveRoomForUserAsync(userId, _clock.UtcNow, cancellationToken);
        if (otherRoom is not null && otherRoom.Id != room.Id)
        {
            throw new ConflictException(Messages.AlreadyInAnotherRoom);
        }

        await _participantRepository.AddAsync(
            new RoomParticipant
            {
                RoomId = room.Id,
                UserId = userId,
                Role = ParticipantRole.Player,
                JoinOrder = participantCount + 1,
                IsReady = false,
                LastSeenAtUtc = _clock.UtcNow
            },
            cancellationToken);

        Room detail = await LoadDetailAsync(room.Id, cancellationToken);
        RoomResponse response = detail.ToResponse(includeJoinCode: true);

        await _notifier.RoomUpdatedAsync(response, cancellationToken);

        return new SuccessDataResult<RoomResponse>(response, Messages.RoomJoined);
    }

    // =====================================================================
    //  Odadan ayrılma
    // =====================================================================
    [TransactionAspect]
    public async Task<IResult> LeaveAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();

        Room room = await _roomRepository.GetAsync(r => r.Id == roomId, asNoTracking: false,
                        cancellationToken: cancellationToken)
                    ?? throw new NotFoundException(Messages.RoomNotFound);

        RoomParticipant participant = await _participantRepository.GetByRoomAndUserAsync(
                                          roomId, userId, asNoTracking: false, cancellationToken)
                                      ?? throw new BusinessException(Messages.NotInRoom);

        if (participant.Role == ParticipantRole.Host)
        {
            // Kurucu ayrılırsa oda sahipsiz kalır. İptal etmesi gerekir.
            throw new BusinessException(Messages.HostCannotLeave);
        }

        if (room.Status != RoomStatus.Waiting)
        {
            throw new BusinessException(Messages.RoomNotWaiting);
        }

        await _participantRepository.DeleteAsync(participant, hardDelete: true, cancellationToken);

        Room detail = await LoadDetailAsync(roomId, cancellationToken);
        await _notifier.RoomUpdatedAsync(detail.ToResponse(includeJoinCode: true), cancellationToken);

        return new SuccessResult(Messages.RoomLeft);
    }

    // =====================================================================
    //  Hazır durumu
    // =====================================================================
    public async Task<IDataResult<RoomResponse>> SetReadyAsync(
        Guid roomId,
        bool isReady,
        CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();

        Room room = await _roomRepository.GetAsync(r => r.Id == roomId, cancellationToken: cancellationToken)
                    ?? throw new NotFoundException(Messages.RoomNotFound);

        if (room.Status != RoomStatus.Waiting)
        {
            throw new BusinessException(Messages.RoomNotWaiting);
        }

        RoomParticipant participant = await _participantRepository.GetByRoomAndUserAsync(
                                          roomId, userId, asNoTracking: false, cancellationToken)
                                      ?? throw new BusinessException(Messages.NotInRoom);

        participant.IsReady = isReady;
        participant.LastSeenAtUtc = _clock.UtcNow;

        await _participantRepository.UpdateAsync(participant, cancellationToken);

        Room detail = await LoadDetailAsync(roomId, cancellationToken);
        RoomResponse response = detail.ToResponse(includeJoinCode: true);

        await _notifier.RoomUpdatedAsync(response, cancellationToken);

        return new SuccessDataResult<RoomResponse>(response);
    }

    // =====================================================================
    //  Yarışmayı başlatma
    // =====================================================================
    [TransactionAspect]
    public async Task<IDataResult<RoomResponse>> StartAsync(
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();

        await StartInternalAsync(roomId, userId, cancellationToken);

        Room detail = await LoadDetailAsync(roomId, cancellationToken);
        RoomResponse response = detail.ToResponse(includeJoinCode: true);

        await _notifier.GameStartedAsync(roomId, cancellationToken);

        return new SuccessDataResult<RoomResponse>(response, Messages.GameStarted);
    }

    /// <remarks>
    /// Yalnızca <c>ScheduledEventStarter</c> tarafından çağrılır.
    /// Ayrıntılı gerekçe için bkz. <see cref="IRoomService.StartScheduledEventAsync"/>.
    /// </remarks>
    [TransactionAspect]
    public async Task<IResult> StartScheduledEventAsync(
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        Room room = await _roomRepository.GetAsync(r => r.Id == roomId, asNoTracking: false,
                        cancellationToken: cancellationToken)
                    ?? throw new NotFoundException(Messages.RoomNotFound);

        // Güvenlik kapısı: bu metot kurucu kontrolü yapmıyor, o yüzden
        // kapsamı daraltmak zorunda. Etkinlik olmayan bir odada asla çalışmaz.
        if (!room.IsOfficialEvent || room.ScheduledStartUtc is null)
        {
            throw new BusinessException(Messages.NotAScheduledEvent);
        }

        // Kurucu kimliğini odanın kendisinden alıyoruz: "istek sahibi kurucu mu"
        // kontrolü böylece bozulmadan geçer ve StartInternalAsync'in tek bir
        // sürümü olur.
        await StartInternalAsync(roomId, room.HostUserId, cancellationToken);

        await _notifier.GameStartedAsync(roomId, cancellationToken);

        _logger.LogInformation("Zamanlanmış etkinlik başlatıldı: {RoomId}", roomId);

        return new SuccessResult(Messages.GameStarted);
    }

    /// <summary>
    /// Yarışma başlatmanın çekirdek mantığı.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Tüm katılımcılar aynı soruları aynı sırada görür.</b> Bu, çok
    /// oyunculu yarışmanın adil olması için zorunlu: farklı soru setleri
    /// alsalardı skorlar karşılaştırılamazdı.
    /// </para>
    /// <para>
    /// Soru seçimi iki adımda yapılır: önce yalnızca kimlikler çekilir, sonra
    /// kriptografik rastgelelikle karıştırılıp ilk N tanesi alınır. Böylece
    /// veritabanına sağlayıcıya özgü rastgeleleme SQL'i (<c>NEWID()</c>)
    /// göndermeye gerek kalmaz.
    /// </para>
    /// </remarks>
    private async Task StartInternalAsync(Guid roomId, Guid requestingUserId, CancellationToken cancellationToken)
    {
        Room room = await _roomRepository.GetAsync(r => r.Id == roomId, asNoTracking: false,
                        cancellationToken: cancellationToken)
                    ?? throw new NotFoundException(Messages.RoomNotFound);

        if (room.HostUserId != requestingUserId)
        {
            throw new ForbiddenException(Messages.OnlyHostCanStart);
        }

        if (room.Status != RoomStatus.Waiting)
        {
            throw new BusinessException(
                room.Status == RoomStatus.InProgress ? Messages.RoomAlreadyStarted : Messages.RoomNotWaiting);
        }

        IReadOnlyList<RoomParticipant> participants =
            await _participantRepository.GetByRoomAsync(roomId, asNoTracking: false, cancellationToken);

        if (participants.Count == 0)
        {
            throw new BusinessException("Odada hiç katılımcı yok.");
        }

        // --- Soru seçimi -----------------------------------------------------
        IReadOnlyList<Guid> availableIds =
            await _questionRepository.GetSelectableQuestionIdsAsync(room.CategoryId, cancellationToken);

        if (availableIds.Count < room.QuestionCount)
        {
            throw new BusinessException(Messages.NotEnoughQuestions);
        }

        Guid[] selectedIds = PickRandom(availableIds, room.QuestionCount);

        IReadOnlyList<Question> questions =
            await _questionRepository.GetWithAnswersByIdsAsync(selectedIds, cancellationToken);

        // Veritabanı IN(...) sonucunu sıralı döndürmez; karıştırdığımız sırayı
        // korumak için sözlük üzerinden yeniden diziyoruz.
        Dictionary<Guid, Question> questionsById = questions.ToDictionary(q => q.Id);
        List<Question> orderedQuestions = selectedIds
            .Where(questionsById.ContainsKey)
            .Select(id => questionsById[id])
            .ToList();

        DateTime now = _clock.UtcNow;

        // --- Her katılımcı için yarışma oturumu ------------------------------
        foreach (RoomParticipant participant in participants)
        {
            var competition = new Competition
            {
                RoomId = room.Id,
                RoomParticipantId = participant.Id,
                Status = CompetitionStatus.InProgress,
                QuestionCount = orderedQuestions.Count,
                StartedAtUtc = now
            };

            await _competitionRepository.AddAsync(competition, cancellationToken);

            var competitionQuestions = orderedQuestions
                .Select((question, index) => new CompetitionQuestion
                {
                    CompetitionId = competition.Id,
                    QuestionId = question.Id,
                    Order = index + 1
                })
                .ToList();

            await _competitionQuestionRepository.AddRangeAsync(competitionQuestions, cancellationToken);
        }

        room.Status = RoomStatus.InProgress;
        room.StartedAtUtc = now;
        await _roomRepository.UpdateAsync(room, cancellationToken);

        // Soru istatistikleri: tek UPDATE ile artırılır.
        await _questionRepository.IncrementAskedStatisticsAsync(selectedIds, cancellationToken);

        _logger.LogInformation(
            "Yarışma başladı: oda {RoomId}, {ParticipantCount} katılımcı, {QuestionCount} soru.",
            room.Id,
            participants.Count,
            orderedQuestions.Count);
    }

    // =====================================================================
    //  Oda iptali
    // =====================================================================
    [TransactionAspect]
    public async Task<IResult> CancelAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();

        Room room = await _roomRepository.GetAsync(r => r.Id == roomId, asNoTracking: false,
                        cancellationToken: cancellationToken)
                    ?? throw new NotFoundException(Messages.RoomNotFound);

        if (room.HostUserId != userId)
        {
            throw new ForbiddenException(Messages.OnlyHostCanCancel);
        }

        if (room.Status is RoomStatus.Finished or RoomStatus.Cancelled)
        {
            throw new BusinessException(Messages.CompetitionAlreadyFinished);
        }

        room.Status = RoomStatus.Cancelled;
        room.FinishedAtUtc = _clock.UtcNow;
        await _roomRepository.UpdateAsync(room, cancellationToken);

        // Devam eden yarışmalar "yarıda bırakıldı" olarak işaretlenir; bu
        // yarışmalar istatistiklere ve sıralamaya dahil edilmez.
        IReadOnlyList<Competition> competitions = await _competitionRepository.GetListAsync(
            c => c.RoomId == roomId && c.Status == CompetitionStatus.InProgress,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        foreach (Competition competition in competitions)
        {
            competition.Status = CompetitionStatus.Abandoned;
            competition.FinishedAtUtc = room.FinishedAtUtc;
            await _competitionRepository.UpdateAsync(competition, cancellationToken);
        }

        await _notifier.RoomCancelledAsync(roomId, cancellationToken);

        return new SuccessResult(Messages.RoomCancelled);
    }

    // =====================================================================
    //  Okuma
    // =====================================================================
    public async Task<IDataResult<RoomResponse>> GetAsync(
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        Room room = await _roomRepository.GetDetailAsync(roomId, cancellationToken: cancellationToken)
                    ?? throw new NotFoundException(Messages.RoomNotFound);

        // Katılım kodu yalnızca odanın içindekilere gösterilir.
        Guid? userId = _currentUser.UserId;
        bool isParticipant = userId is not null && room.Participants.Any(p => p.UserId == userId);

        return new SuccessDataResult<RoomResponse>(room.ToResponse(includeJoinCode: isParticipant));
    }

    public async Task<IDataResult<PagedResponse<RoomSummaryResponse>>> GetJoinableAsync(
        PageRequest pageRequest,
        CancellationToken cancellationToken = default)
    {
        PagedList<Room> page = await _roomRepository.GetJoinableRoomsAsync(pageRequest, cancellationToken);
        PagedList<RoomSummaryResponse> mapped = page.Map(r => r.ToSummaryResponse());

        return new SuccessDataResult<PagedResponse<RoomSummaryResponse>>(
            PagedResponse<RoomSummaryResponse>.From(mapped));
    }

    public async Task<IDataResult<RoomResponse?>> GetMyActiveRoomAsync(CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();

        Room? room = await _roomRepository.GetActiveRoomForUserAsync(userId, _clock.UtcNow, cancellationToken);

        if (room is null)
        {
            // 'data:' adlandırılmış argüman zorunlu: aksi hâlde null değeri
            // (T?) ve (string message) aşırı yüklemeleri arasında belirsiz kalır.
            return new SuccessDataResult<RoomResponse?>(data: null);
        }

        Room detail = await LoadDetailAsync(room.Id, cancellationToken);

        return new SuccessDataResult<RoomResponse?>(detail.ToResponse(includeJoinCode: true));
    }

    // =====================================================================
    //  Yardımcılar
    // =====================================================================
    private async Task<Room> LoadDetailAsync(Guid roomId, CancellationToken cancellationToken)
        => await _roomRepository.GetDetailAsync(roomId, cancellationToken: cancellationToken)
           ?? throw new NotFoundException(Messages.RoomNotFound);

    private async Task<string> GenerateUniqueJoinCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < JoinCodeMaxAttempts; attempt++)
        {
            string code = JoinCodeGenerator.Generate();

            if (!await _roomRepository.JoinCodeExistsAsync(code, cancellationToken))
            {
                return code;
            }
        }

        // 26^6 ≈ 309 milyon olasılıkta 10 denemenin tamamının çakışması
        // pratikte imkânsız; buraya düşmek veri bütünlüğü sorununa işaret eder.
        throw new BusinessException("Katılım kodu üretilemedi, lütfen tekrar deneyin.");
    }

    /// <summary>
    /// Listeden kriptografik rastgelelikle <paramref name="count"/> öğe seçer.
    /// </summary>
    /// <remarks>
    /// Kısmi Fisher-Yates: yalnızca ihtiyaç duyulan kadar adım atar
    /// (O(count), tüm listeyi karıştırmaz). <c>Random</c> yerine
    /// <see cref="RandomNumberGenerator"/> kullanılıyor; böylece aynı saniyede
    /// başlayan iki yarışma aynı tohumla aynı soru setini almaz ve soru sırası
    /// tahmin edilemez.
    /// </remarks>
    private static Guid[] PickRandom(IReadOnlyList<Guid> source, int count)
    {
        Guid[] pool = [.. source];
        int take = Math.Min(count, pool.Length);

        for (var i = 0; i < take; i++)
        {
            int j = RandomNumberGenerator.GetInt32(i, pool.Length);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        return pool[..take];
    }
}
