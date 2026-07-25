using System.Security.Claims;
using QuizArena.BLL.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace QuizArena.Api.Hubs;

/// <summary>
/// Gerçek zamanlı yarışma kanalı.
/// </summary>
/// <remarks>
/// <para>
/// Hub <b>hiçbir iş kuralı içermez</b>: yalnızca istemcileri oda gruplarına
/// alır/çıkarır. Puanlama, cevap doğrulama ve durum geçişleri HTTP uçlarından
/// <see cref="IGameService"/> ve <see cref="IRoomService"/> üzerinden yapılır.
/// </para>
/// <para>
/// <b>Neden bu ayrım?</b> Hub metotları da bir giriş noktasıdır; iş kuralı
/// hub'a yazılsaydı aynı kural iki yerde (HTTP + WebSocket) tekrarlanmak
/// zorunda kalırdı ve ikisi zamanla ayrışırdı. Ayrıca istemcinin WebSocket
/// bağlantısı koptuğunda oyunun ilerlemesi imkânsız hâle gelirdi.
/// </para>
/// <para>
/// <c>[Authorize]</c> zorunlu: kimliği doğrulanmamış bir istemci oda grubuna
/// katılıp diğer oyuncuların skorlarını canlı izleyebilirdi.
/// </para>
/// </remarks>
[Authorize]
public sealed class QuizHub : Hub
{
    private readonly IRoomService _roomService;
    private readonly ILogger<QuizHub> _logger;

    public QuizHub(IRoomService roomService, ILogger<QuizHub> logger)
    {
        _roomService = roomService;
        _logger = logger;
    }

    /// <summary>SignalR grup adı: oda başına bir grup.</summary>
    public static string RoomGroup(Guid roomId) => $"room:{roomId}";

    /// <summary>
    /// İstemciyi bir odanın canlı yayın grubuna alır.
    /// </summary>
    /// <remarks>
    /// <b>Yetki kontrolü burada da yapılır:</b> istemcinin gönderdiği oda
    /// kimliğine körlemesine güvenilmez. Kullanıcı gerçekten o odanın
    /// katılımcısı mı diye <see cref="IRoomService"/>'e sorulur; aksi hâlde
    /// herhangi bir oyuncu, rastgele oda kimlikleri deneyerek başkalarının
    /// yarışmasını canlı izleyebilirdi.
    /// </remarks>
    public async Task JoinRoomChannel(Guid roomId)
    {
        Guid? userId = GetUserId();
        if (userId is null)
        {
            throw new HubException("Kimlik doğrulanamadı.");
        }

        var room = await _roomService.GetAsync(roomId, Context.ConnectionAborted);

        bool isParticipant = room.Data?.Participants.Any(p => p.UserId == userId) ?? false;
        if (!isParticipant)
        {
            throw new HubException("Bu odanın katılımcısı değilsiniz.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(roomId), Context.ConnectionAborted);

        _logger.LogDebug("Bağlantı {ConnectionId} oda kanalına katıldı: {RoomId}", Context.ConnectionId, roomId);
    }

    public Task LeaveRoomChannel(Guid roomId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroup(roomId), Context.ConnectionAborted);

    private Guid? GetUserId()
    {
        string? raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out Guid id) ? id : null;
    }
}
