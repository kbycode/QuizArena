using QuizArena.Core.DataAccess.Paging;
using QuizArena.Core.Utilities.Results;
using QuizArena.Entities.Dtos.Common;
using QuizArena.Entities.Dtos.Rooms;

namespace QuizArena.BLL.Abstract;

/// <summary>
/// Zamanlanmış etkinlikler (turnuvalar).
/// </summary>
/// <remarks>
/// Etkinlik, arkasında sıradan bir <c>Room</c> bulunan zamanlanmış bir
/// yarışmadır. Bu servis yalnızca <b>etkinliğe özgü</b> davranışı yönetir:
/// takvim, kayıt/iptal ve zamanı gelenlerin otomatik başlatılması. Yarışmanın
/// kendisi (soru dağıtımı, puanlama) <see cref="IRoomService"/> ve
/// <see cref="IGameService"/> tarafından yürütülür.
/// </remarks>
public interface IEventService
{
    /// <summary>Yaklaşan etkinlikler (oyuncu görünümü).</summary>
    Task<IDataResult<IReadOnlyList<EventResponse>>> GetUpcomingAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Tüm etkinlikler (yönetim görünümü, sayfalı).</summary>
    Task<IDataResult<PagedResponse<EventResponse>>> GetForAdminAsync(
        PageRequest pageRequest,
        CancellationToken cancellationToken = default);

    Task<IDataResult<EventResponse>> CreateAsync(
        SaveEventRequest request,
        CancellationToken cancellationToken = default);

    Task<IDataResult<EventResponse>> UpdateAsync(
        Guid eventId,
        SaveEventRequest request,
        CancellationToken cancellationToken = default);

    Task<IResult> CancelAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Oyuncuyu etkinliğe kaydeder.</summary>
    Task<IDataResult<EventResponse>> RegisterAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>Oyuncunun kaydını geri alır.</summary>
    Task<IDataResult<EventResponse>> WithdrawAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saati gelmiş etkinlikleri başlatır; katılımcısı olmayanları iptal eder.
    /// </summary>
    /// <remarks>
    /// Arka plan hizmeti (<c>ScheduledEventStarter</c>) tarafından düzenli
    /// aralıklarla çağrılır. HTTP üzerinden erişilebilir bir ucu yoktur.
    /// </remarks>
    /// <returns>Başlatılan etkinlik sayısı.</returns>
    Task<int> ProcessDueEventsAsync(CancellationToken cancellationToken = default);
}
