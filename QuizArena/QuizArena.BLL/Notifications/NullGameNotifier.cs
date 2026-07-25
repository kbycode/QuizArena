using QuizArena.Entities.Dtos.Play;
using QuizArena.Entities.Dtos.Rooms;

namespace QuizArena.BLL.Notifications;

/// <summary>
/// Hiçbir şey yapmayan bildirici (Null Object deseni).
/// </summary>
/// <remarks>
/// Gerçek zamanlı altyapı olmayan bir barındırma senaryosunda (ör. yalnızca
/// iş kurallarını çalıştıran bir konsol aracı veya birim testi) devreye girer.
/// Alternatifi, iş kodunun her yerinde <c>_notifier?.…</c> yazmak ya da
/// <c>null</c> kontrolü yapmaktı; Null Object deseni bu gürültüyü ortadan
/// kaldırır ve "bildirim gönderilmedi" durumunu bir hata değil, geçerli bir
/// yapılandırma yapar.
/// </remarks>
public sealed class NullGameNotifier : IGameNotifier
{
    public Task RoomUpdatedAsync(RoomResponse room, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task GameStartedAsync(Guid roomId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task ScoreboardUpdatedAsync(
        Guid roomId,
        IReadOnlyList<ScoreboardEntryResponse> scoreboard,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task GameFinishedAsync(
        Guid roomId,
        IReadOnlyList<ScoreboardEntryResponse> finalScoreboard,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RoomCancelledAsync(Guid roomId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
