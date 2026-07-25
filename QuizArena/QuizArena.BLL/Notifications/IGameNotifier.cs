using QuizArena.Entities.Dtos.Play;
using QuizArena.Entities.Dtos.Rooms;

namespace QuizArena.BLL.Notifications;

/// <summary>
/// Oyun olaylarını odadaki istemcilere anlık olarak duyurma soyutlaması.
/// </summary>
/// <remarks>
/// <para>
/// <b>Bu arayüz mimarinin en önemli sınırlarından birini çizer.</b> Gerçek
/// zamanlı bildirim SignalR ile yapılıyor; ancak <c>IHubContext</c> tipini
/// iş katmanına sokmak, BLL'i ASP.NET Core SignalR'a bağımlı hâle getirirdi.
/// Sonuçları:
/// </para>
/// <list type="bullet">
///   <item>Birim testinde <c>IHubContext</c> taklit etmek zorunda kalınır (zahmetli ve kırılgan).</item>
///   <item>Yarın bildirim WebSocket yerine kuyruk/push ile yapılacaksa iş kuralları değişmek zorunda kalır.</item>
///   <item>Katman bağımlılığı tersine döner: iş kuralı sunum teknolojisini tanır.</item>
/// </list>
/// <para>
/// Somut uygulaması (<c>SignalRGameNotifier</c>) API katmanındadır. Bildirim
/// altyapısı hiç kaydedilmezse, hiçbir şey yapmayan bir uygulama (null object)
/// devreye girer ve oyun akışı bozulmadan çalışmaya devam eder.
/// </para>
/// </remarks>
public interface IGameNotifier
{
    /// <summary>Odaya yeni katılım/ayrılma sonrası güncel oda durumu.</summary>
    Task RoomUpdatedAsync(RoomResponse room, CancellationToken cancellationToken = default);

    /// <summary>Yarışma başladı; istemciler ilk soruyu istemeye başlayabilir.</summary>
    Task GameStartedAsync(Guid roomId, CancellationToken cancellationToken = default);

    /// <summary>Bir oyuncu cevap verdi; skor tablosu güncellendi.</summary>
    Task ScoreboardUpdatedAsync(
        Guid roomId,
        IReadOnlyList<ScoreboardEntryResponse> scoreboard,
        CancellationToken cancellationToken = default);

    /// <summary>Odadaki tüm yarışmalar bitti.</summary>
    Task GameFinishedAsync(
        Guid roomId,
        IReadOnlyList<ScoreboardEntryResponse> finalScoreboard,
        CancellationToken cancellationToken = default);

    /// <summary>Oda iptal edildi; istemciler ekranı kapatmalı.</summary>
    Task RoomCancelledAsync(Guid roomId, CancellationToken cancellationToken = default);
}
