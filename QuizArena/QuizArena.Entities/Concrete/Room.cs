using QuizArena.Core.Entities;
using QuizArena.Core.Entities.Concrete;
using QuizArena.Entities.Enums;

namespace QuizArena.Entities.Concrete;

/// <summary>
/// Yarışma odası: oyuncuların buluştuğu ve ayarların belirlendiği yer.
/// </summary>
public class Room : EntityBase
{
    public Guid CategoryId { get; set; }

    /// <summary>Odayı kuran kullanıcı. Yarışmayı başlatma yetkisi ona aittir.</summary>
    public Guid HostUserId { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>
    /// 6 haneli katılım kodu (örn. <c>7K4M2P</c>). Arkadaşa gönderilip
    /// odaya katılmak için kullanılır.
    /// <para>
    /// Oda kimliği olan GUID yerine kısa kod kullanılması bilinçli: GUID'i
    /// telefonda okutmak/yazmak pratik değil. Kodun kısa olması, tahmin
    /// edilebilirliğini artırdığı için kod üretiminde karıştırılabilir
    /// karakterler (0/O, 1/I) dışlanır ve kod yalnızca <c>Waiting</c>
    /// durumundaki odalar için geçerlidir.
    /// </para>
    /// </summary>
    public string JoinCode { get; set; } = null!;

    public RoomMode Mode { get; set; }

    public RoomStatus Status { get; set; } = RoomStatus.Waiting;

    public int QuestionCount { get; set; }

    /// <summary>Soru başına tanınan süre. Sorunun kendi süresi bunu ezebilir.</summary>
    public int SecondsPerQuestion { get; set; }

    public int MaxPlayers { get; set; }

    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    public Category Category { get; set; } = null!;
    public User HostUser { get; set; } = null!;

    public ICollection<RoomParticipant> Participants { get; set; } = [];
    public ICollection<Competition> Competitions { get; set; } = [];

    public bool IsMultiplayer => Mode != RoomMode.Solo;
}
