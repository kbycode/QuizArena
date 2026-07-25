using QuizArena.Core.DataAccess.Paging;
using QuizArena.Core.Utilities.Results;
using QuizArena.Entities.Dtos.Common;
using QuizArena.Entities.Dtos.Rooms;

namespace QuizArena.BLL.Abstract;

/// <summary>Oda yaşam döngüsü: kur, katıl, hazırlan, başlat, ayrıl, iptal et.</summary>
public interface IRoomService
{
    /// <summary>
    /// Oda kurar. Tek kişilik (<c>Solo</c>) kipte yarışma <b>aynı işlemde</b>
    /// başlatılır; oyuncu ekstra bir adım atmak zorunda kalmaz.
    /// </summary>
    Task<IDataResult<RoomResponse>> CreateAsync(
        CreateRoomRequest request,
        CancellationToken cancellationToken = default);

    Task<IDataResult<RoomResponse>> JoinAsync(
        JoinRoomRequest request,
        CancellationToken cancellationToken = default);

    Task<IResult> LeaveAsync(Guid roomId, CancellationToken cancellationToken = default);

    /// <summary>Çok oyunculu odada "hazırım" işaretini değiştirir.</summary>
    Task<IDataResult<RoomResponse>> SetReadyAsync(
        Guid roomId,
        bool isReady,
        CancellationToken cancellationToken = default);

    /// <summary>Yarışmayı başlatır: her katılımcı için soru seti üretilir.</summary>
    Task<IDataResult<RoomResponse>> StartAsync(Guid roomId, CancellationToken cancellationToken = default);

    Task<IResult> CancelAsync(Guid roomId, CancellationToken cancellationToken = default);

    Task<IDataResult<RoomResponse>> GetAsync(Guid roomId, CancellationToken cancellationToken = default);

    /// <summary>Katılıma açık genel odalar.</summary>
    Task<IDataResult<PagedResponse<RoomSummaryResponse>>> GetJoinableAsync(
        PageRequest pageRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının devam eden odası. Sayfa yenilendiğinde oyuna geri
    /// dönebilmesi için gerekli.
    /// </summary>
    Task<IDataResult<RoomResponse?>> GetMyActiveRoomAsync(CancellationToken cancellationToken = default);
}
