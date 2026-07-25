using QuizArena.Core.Utilities.Results;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Dtos.Statistics;

namespace QuizArena.BLL.Abstract;

/// <summary>Kullanıcı istatistikleri.</summary>
public interface IStatisticService
{
    Task<IDataResult<UserStatisticResponse>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IDataResult<UserStatisticResponse>> GetMyStatisticsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tamamlanan bir yarışmanın sonucunu birikimli istatistiklere işler.
    /// </summary>
    /// <remarks>
    /// <see cref="IGameService"/> tarafından, yarışmayı bitiren işlemin
    /// <b>içinde</b> (aynı transaction'da) çağrılır. Ayrı bir uçtan
    /// erişilebilir olması amaçlanmamıştır.
    /// </remarks>
    Task<UserStatistic> ApplyCompetitionResultAsync(
        Competition competition,
        Guid userId,
        int averageAnswerMilliseconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Çok oyunculu bir yarışmanın birincisine galibiyet yazar.
    /// </summary>
    /// <remarks>
    /// Ayrı bir metot olması gerekiyor çünkü birinci, <b>son oyuncu bitene
    /// kadar</b> belli değildir; bu yüzden istatistik güncellemesinden ayrı
    /// bir anda çağrılır.
    /// </remarks>
    Task RegisterWinAsync(Guid userId, CancellationToken cancellationToken = default);
}
