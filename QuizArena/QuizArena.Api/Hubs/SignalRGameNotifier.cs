using QuizArena.BLL.Notifications;
using QuizArena.Entities.Dtos.Play;
using QuizArena.Entities.Dtos.Rooms;
using Microsoft.AspNetCore.SignalR;

namespace QuizArena.Api.Hubs;

/// <summary>
/// <see cref="IGameNotifier"/>'ın SignalR uygulaması.
/// </summary>
/// <remarks>
/// <para>
/// <b>Mimari olarak bu sınıfın yeri önemli:</b> SignalR'a bağımlı olan tek
/// parça burada, API katmanında. İş katmanı yalnızca <see cref="IGameNotifier"/>
/// arayüzünü tanıyor. Yarın bildirimler Web Push veya bir mesaj kuyruğuyla
/// gönderilecekse tek yapılacak iş bu sınıfın yerine yenisini yazmak.
/// </para>
/// <para>
/// Bildirim gönderimi <b>hata yutar</b>: bir istemciye mesaj ulaşmaması,
/// oyuncunun cevabının kaydedilmemesi anlamına gelmemeli. Bildirim en iyi
/// çaba (best-effort) bir yan etkidir; asıl doğruluk kaynağı veritabanıdır ve
/// istemci her durumda HTTP ile güncel durumu çekebilir.
/// </para>
/// </remarks>
public sealed class SignalRGameNotifier : IGameNotifier
{
    private readonly IHubContext<QuizHub> _hubContext;
    private readonly ILogger<SignalRGameNotifier> _logger;

    public SignalRGameNotifier(IHubContext<QuizHub> hubContext, ILogger<SignalRGameNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task RoomUpdatedAsync(RoomResponse room, CancellationToken cancellationToken = default)
        => SendAsync(room.Id, "RoomUpdated", room, cancellationToken);

    public Task GameStartedAsync(Guid roomId, CancellationToken cancellationToken = default)
        => SendAsync(roomId, "GameStarted", roomId, cancellationToken);

    public Task ScoreboardUpdatedAsync(
        Guid roomId,
        IReadOnlyList<ScoreboardEntryResponse> scoreboard,
        CancellationToken cancellationToken = default)
        => SendAsync(roomId, "ScoreboardUpdated", scoreboard, cancellationToken);

    public Task GameFinishedAsync(
        Guid roomId,
        IReadOnlyList<ScoreboardEntryResponse> finalScoreboard,
        CancellationToken cancellationToken = default)
        => SendAsync(roomId, "GameFinished", finalScoreboard, cancellationToken);

    public Task RoomCancelledAsync(Guid roomId, CancellationToken cancellationToken = default)
        => SendAsync(roomId, "RoomCancelled", roomId, cancellationToken);

    private async Task SendAsync(Guid roomId, string method, object payload, CancellationToken cancellationToken)
    {
        try
        {
            await _hubContext.Clients
                .Group(QuizHub.RoomGroup(roomId))
                .SendAsync(method, payload, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Bilinçli yutma: bildirim gönderilemedi diye tamamlanmış bir
            // yarışma işlemi geri alınmamalı.
            _logger.LogWarning(
                exception,
                "Gerçek zamanlı bildirim gönderilemedi: {Method} (oda {RoomId})",
                method,
                roomId);
        }
    }
}
