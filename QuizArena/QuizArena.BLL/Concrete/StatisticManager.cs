using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.BLL.Mapping;
using QuizArena.Core.Entities.Concrete;
using QuizArena.Core.Exceptions;
using QuizArena.Core.Utilities.Clock;
using QuizArena.Core.Utilities.Results;
using QuizArena.Core.Utilities.Security;
using QuizArena.DAL.Abstract;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Dtos.Statistics;

namespace QuizArena.BLL.Concrete;

/// <summary>Kullanıcı istatistikleri.</summary>
public sealed class StatisticManager : IStatisticService
{
    private readonly IUserStatisticRepository _statisticRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAchievementService _achievementService;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;

    public StatisticManager(
        IUserStatisticRepository statisticRepository,
        IUserRepository userRepository,
        IAchievementService achievementService,
        ICurrentUserService currentUser,
        IClock clock)
    {
        _statisticRepository = statisticRepository;
        _userRepository = userRepository;
        _achievementService = achievementService;
        _currentUser = currentUser;
        _clock = clock;
    }

    public Task<IDataResult<UserStatisticResponse>> GetMyStatisticsAsync(
        CancellationToken cancellationToken = default)
        => GetForUserAsync(_currentUser.RequireUserId(), cancellationToken);

    public async Task<IDataResult<UserStatisticResponse>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        User user = await _userRepository.GetAsync(u => u.Id == userId, cancellationToken: cancellationToken)
                    ?? throw new NotFoundException(Messages.UserNotFound);

        // İstatistik satırı kayıt sırasında oluşturulur; yine de eksikse
        // (ör. veri elle eklenmişse) boş bir nesneyle devam ediyoruz —
        // profil sayfası bu yüzden hata vermemeli.
        UserStatistic statistic = await _statisticRepository.GetByUserAsync(userId, cancellationToken: cancellationToken)
                                  ?? new UserStatistic { UserId = userId };

        IDataResult<IReadOnlyList<AchievementResponse>> achievements =
            await _achievementService.GetForUserAsync(userId, cancellationToken);

        return new SuccessDataResult<UserStatisticResponse>(
            statistic.ToResponse(user, achievements.Data ?? []));
    }

    /// <remarks>
    /// <para>
    /// Bu metot <see cref="IGameService"/>'in yarışma bitirme işleminin
    /// <b>içinde</b>, aynı transaction'da çağrılır. Bu yüzden burada ayrıca
    /// transaction açılmıyor.
    /// </para>
    /// <para>
    /// Ortalama cevap süresi <b>ağırlıklı</b> güncellenir: eski ortalama eski
    /// soru sayısıyla, yeni ortalama yeni soru sayısıyla çarpılıp toplam soru
    /// sayısına bölünür. Basitçe iki ortalamanın ortalamasını almak, 3 soruluk
    /// bir yarışmayı 300 soruluk geçmişle eşit ağırlıkta sayardı.
    /// </para>
    /// </remarks>
    public async Task<UserStatistic> ApplyCompetitionResultAsync(
        Competition competition,
        Guid userId,
        int averageAnswerMilliseconds,
        CancellationToken cancellationToken = default)
    {
        UserStatistic? statistic = await _statisticRepository.GetByUserAsync(
            userId, asNoTracking: false, cancellationToken);

        if (statistic is null)
        {
            statistic = new UserStatistic { UserId = userId };
            await _statisticRepository.AddAsync(statistic, cancellationToken);
        }

        int previousAnswered = statistic.TotalQuestionsAnswered;
        int newAnswered = competition.AnsweredCount;

        statistic.TotalCompetitions++;
        statistic.TotalQuestionsAnswered += newAnswered;
        statistic.TotalCorrectAnswers += competition.CorrectCount;
        statistic.TotalScore += competition.TotalScore;
        statistic.BestStreak = Math.Max(statistic.BestStreak, competition.LongestStreak);
        statistic.BestCompetitionScore = Math.Max(statistic.BestCompetitionScore, competition.TotalScore);
        statistic.LastPlayedAtUtc = _clock.UtcNow;

        statistic.AverageAnswerMilliseconds = CalculateWeightedAverage(
            statistic.AverageAnswerMilliseconds,
            previousAnswered,
            averageAnswerMilliseconds,
            newAnswered);

        await _statisticRepository.UpdateAsync(statistic, cancellationToken);

        return statistic;
    }

    public async Task RegisterWinAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        UserStatistic? statistic = await _statisticRepository.GetByUserAsync(
            userId, asNoTracking: false, cancellationToken);

        if (statistic is null)
        {
            return;
        }

        statistic.WinCount++;
        await _statisticRepository.UpdateAsync(statistic, cancellationToken);
    }

    private static int CalculateWeightedAverage(
        int previousAverage,
        int previousCount,
        int newAverage,
        int newCount)
    {
        int totalCount = previousCount + newCount;

        if (totalCount == 0)
        {
            return 0;
        }

        long weightedSum = (long)previousAverage * previousCount + (long)newAverage * newCount;

        return (int)(weightedSum / totalCount);
    }
}
