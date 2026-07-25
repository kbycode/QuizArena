using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.BLL.Mapping;
using QuizArena.Core.Aspects.Caching;
using QuizArena.Core.Utilities.Results;
using QuizArena.Core.Utilities.Security;
using QuizArena.DAL.Abstract;
using QuizArena.DAL.ReadModels;
using QuizArena.Entities.Dtos.Statistics;

namespace QuizArena.BLL.Concrete;

/// <summary>Genel sıralama tablosu.</summary>
public sealed class LeaderboardManager : ILeaderboardService
{
    private readonly IUserStatisticRepository _statisticRepository;
    private readonly ICurrentUserService _currentUser;

    public LeaderboardManager(IUserStatisticRepository statisticRepository, ICurrentUserService currentUser)
    {
        _statisticRepository = statisticRepository;
        _currentUser = currentUser;
    }

    /// <remarks>
    /// <para>
    /// Sıralama tablosu, uygulamanın en sık okunan ve en pahalı sorgusudur;
    /// buna karşılık <b>saniye saniye güncel olması gerekmez</b>. Kısa süreli
    /// (2 dakika) önbellek, ana sayfa yüklerinin neredeyse tamamını
    /// veritabanına gitmeden karşılar.
    /// </para>
    /// <para>
    /// <c>VaryByUser</c> <b>bilinçli olarak açılmadı</b>: bu liste herkes için
    /// aynıdır. Kullanıcıya özel olan tek şey <see cref="GetMyRankAsync"/> ve o
    /// önbelleklenmiyor.
    /// </para>
    /// </remarks>
    [CacheAspect(KeyPrefix = CacheKeys.Leaderboard, DurationMinutes = GameRules.LeaderboardCacheMinutes)]
    public async Task<IDataResult<IReadOnlyList<LeaderboardEntryResponse>>> GetTopAsync(
        int top,
        CancellationToken cancellationToken = default)
    {
        // İstemcinin gönderdiği sayı sınırlanır: "top=1000000" ile
        // veritabanını yormak mümkün olmasın.
        int limit = Math.Clamp(
            top <= 0 ? GameRules.LeaderboardDefaultTop : top,
            1,
            GameRules.LeaderboardMaxTop);

        IReadOnlyList<LeaderboardRow> rows = await _statisticRepository.GetLeaderboardAsync(limit, cancellationToken);

        IReadOnlyList<LeaderboardEntryResponse> response = rows
            .Select((row, index) => row.ToResponse(index + 1))
            .ToArray();

        return new SuccessDataResult<IReadOnlyList<LeaderboardEntryResponse>>(response);
    }

    public async Task<IDataResult<int>> GetMyRankAsync(CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();
        int rank = await _statisticRepository.GetRankAsync(userId, cancellationToken);

        return new SuccessDataResult<int>(rank);
    }
}
