using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.BLL.Mapping;
using QuizArena.Core.Aspects.Caching;
using QuizArena.Core.Utilities.Clock;
using QuizArena.Core.Utilities.Results;
using QuizArena.DAL.Abstract;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Dtos.Statistics;
using QuizArena.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace QuizArena.BLL.Concrete;

/// <summary>
/// Rozet (başarım) değerlendirme.
/// </summary>
/// <remarks>
/// <para>
/// <b>Tasarım:</b> rozetin <b>metni</b> veritabanında, <b>kazanma koşulu</b>
/// kodda durur. Koşulu da veriye taşımak (ör. bir kural motoru) esneklik gibi
/// görünür ama "üst üste 10 doğru" gibi bir kuralı veriyle ifade etmek,
/// okunması ve test edilmesi çok daha zor bir yapı üretir. Metinlerin veride
/// olması ise çeviri/düzeltme için yeniden derleme gerektirmez.
/// </para>
/// <para>
/// Aynı rozetin iki kez verilmesi <c>(UserId, AchievementId)</c> tekil
/// indeksiyle veritabanı seviyesinde de engellenir.
/// </para>
/// </remarks>
public sealed class AchievementManager : IAchievementService
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IUserAchievementRepository _userAchievementRepository;
    private readonly IClock _clock;
    private readonly ILogger<AchievementManager> _logger;

    public AchievementManager(
        IAchievementRepository achievementRepository,
        IUserAchievementRepository userAchievementRepository,
        IClock clock,
        ILogger<AchievementManager> logger)
    {
        _achievementRepository = achievementRepository;
        _userAchievementRepository = userAchievementRepository;
        _clock = clock;
        _logger = logger;
    }

    [CacheAspect(KeyPrefix = CacheKeys.Achievements, DurationMinutes = 60)]
    public async Task<IDataResult<IReadOnlyList<AchievementResponse>>> GetCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Achievement> achievements = await _achievementRepository.GetListAsync(
            orderBy: q => q.OrderBy(a => a.Code),
            cancellationToken: cancellationToken);

        IReadOnlyList<AchievementResponse> response = achievements.Select(a => a.ToResponse()).ToArray();

        return new SuccessDataResult<IReadOnlyList<AchievementResponse>>(response);
    }

    public async Task<IDataResult<IReadOnlyList<AchievementResponse>>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<UserAchievement> earned =
            await _userAchievementRepository.GetByUserWithAchievementAsync(userId, cancellationToken);

        IReadOnlyList<AchievementResponse> response = earned.Select(ua => ua.ToResponse()).ToArray();

        return new SuccessDataResult<IReadOnlyList<AchievementResponse>>(response);
    }

    public async Task<IReadOnlyList<AchievementResponse>> GetEarnedInCompetitionAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<UserAchievement> earned =
            await _userAchievementRepository.GetByCompetitionAsync(competitionId, cancellationToken);

        return earned.Select(ua => ua.ToResponse()).ToArray();
    }

    /// <summary>
    /// Yarışma bitiminde kazanılan rozetleri belirler.
    /// </summary>
    /// <remarks>
    /// Şampiyonluk rozeti burada değerlendirilmez: birinci, odadaki
    /// <b>son</b> oyuncu bitene kadar bilinemez. O rozet
    /// <see cref="GrantAsync"/> ile ayrıca verilir.
    /// </remarks>
    public async Task<IReadOnlyList<AchievementResponse>> EvaluateAsync(
        Competition competition,
        Guid userId,
        UserStatistic statistic,
        bool hasFastCorrectAnswer,
        CancellationToken cancellationToken = default)
    {
        var candidates = new List<AchievementCode>();

        if (statistic.TotalCompetitions >= 1)
        {
            candidates.Add(AchievementCode.FirstBlood);
        }

        // Kusursuz: bütün sorular doğru. Soru sayısı 0 olan bir yarışmada
        // "hepsini bildi" saymak yanlış olurdu.
        if (competition.QuestionCount > 0 && competition.CorrectCount == competition.QuestionCount)
        {
            candidates.Add(AchievementCode.Perfectionist);
        }

        if (hasFastCorrectAnswer)
        {
            candidates.Add(AchievementCode.QuickThinker);
        }

        if (statistic.TotalCompetitions >= GameRules.VeteranCompetitionCount)
        {
            candidates.Add(AchievementCode.Veteran);
        }

        if (competition.LongestStreak >= GameRules.StreakMasterThreshold)
        {
            candidates.Add(AchievementCode.StreakMaster);
        }

        return await GrantManyAsync(userId, candidates, competition.Id, cancellationToken);
    }

    public async Task<AchievementResponse?> GrantAsync(
        Guid userId,
        AchievementCode code,
        Guid? competitionId,
        CancellationToken cancellationToken = default)
    {
        AchievementResponse[] granted =
            await GrantManyAsync(userId, [code], competitionId, cancellationToken);

        return granted.Length > 0 ? granted[0] : null;
    }

    /// <summary>
    /// Aday rozetlerden kullanıcıda <b>olmayanları</b> verir.
    /// </summary>
    /// <remarks>
    /// Kazanılmış kodlar ve rozet tanımları <b>tek sorguda</b> çekilir; aday
    /// başına ayrı sorgu atılmaz (N+1 önlemi).
    /// </remarks>
    private async Task<AchievementResponse[]> GrantManyAsync(
        Guid userId,
        List<AchievementCode> candidates,
        Guid? competitionId,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        IReadOnlyList<AchievementCode> alreadyEarned =
            await _userAchievementRepository.GetEarnedCodesAsync(userId, cancellationToken);

        AchievementCode[] missing = candidates.Distinct().Except(alreadyEarned).ToArray();

        if (missing.Length == 0)
        {
            return [];
        }

        IReadOnlyList<Achievement> definitions =
            await _achievementRepository.GetByCodesAsync(missing, cancellationToken);

        if (definitions.Count == 0)
        {
            return [];
        }

        DateTime now = _clock.UtcNow;

        var records = definitions
            .Select(definition => new UserAchievement
            {
                UserId = userId,
                AchievementId = definition.Id,
                CompetitionId = competitionId,
                EarnedAtUtc = now
            })
            .ToList();

        await _userAchievementRepository.AddRangeAsync(records, cancellationToken);

        _logger.LogInformation(
            "Kullanıcı {UserId} yeni rozet kazandı: {Codes}",
            userId,
            string.Join(", ", definitions.Select(d => d.Code)));

        return definitions.Select(d => d.ToResponse(now)).ToArray();
    }
}
