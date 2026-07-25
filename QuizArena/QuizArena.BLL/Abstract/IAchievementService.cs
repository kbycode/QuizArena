using QuizArena.Core.Utilities.Results;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Dtos.Statistics;

namespace QuizArena.BLL.Abstract;

/// <summary>Rozet değerlendirme ve listeleme.</summary>
public interface IAchievementService
{
    /// <summary>Tüm rozet tanımları (kazanılma bilgisi olmadan). Önbelleklenir.</summary>
    Task<IDataResult<IReadOnlyList<AchievementResponse>>> GetCatalogAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının kazandığı rozetler.</summary>
    Task<IDataResult<IReadOnlyList<AchievementResponse>>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Yarışma bitiminde kazanılan yeni rozetleri belirler ve kaydeder.
    /// </summary>
    /// <returns>Bu yarışmada <b>yeni</b> kazanılan rozetler.</returns>
    Task<IReadOnlyList<AchievementResponse>> EvaluateAsync(
        Competition competition,
        Guid userId,
        UserStatistic statistic,
        bool hasFastCorrectAnswer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tek bir rozeti verir. Kullanıcı rozete zaten sahipse <c>null</c> döner.
    /// </summary>
    Task<AchievementResponse?> GrantAsync(
        Guid userId,
        Entities.Enums.AchievementCode code,
        Guid? competitionId,
        CancellationToken cancellationToken = default);

    /// <summary>Belirli bir yarışmada kazanılmış rozetler (sonuç ekranı için).</summary>
    Task<IReadOnlyList<AchievementResponse>> GetEarnedInCompetitionAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default);
}
