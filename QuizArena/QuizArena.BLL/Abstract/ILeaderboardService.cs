using QuizArena.Core.Utilities.Results;
using QuizArena.Entities.Dtos.Statistics;

namespace QuizArena.BLL.Abstract;

public interface ILeaderboardService
{
    /// <summary>Genel sıralama tablosu. Kısa süreli önbelleklenir.</summary>
    Task<IDataResult<IReadOnlyList<LeaderboardEntryResponse>>> GetTopAsync(
        int top,
        CancellationToken cancellationToken = default);

    /// <summary>Oturum açmış kullanıcının sıralamadaki yeri (0 = henüz yok).</summary>
    Task<IDataResult<int>> GetMyRankAsync(CancellationToken cancellationToken = default);
}
