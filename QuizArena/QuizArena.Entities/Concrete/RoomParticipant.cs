using QuizArena.Core.Entities;
using QuizArena.Core.Entities.Concrete;
using QuizArena.Entities.Enums;

namespace QuizArena.Entities.Concrete;

/// <summary>
/// Bir kullanıcının belirli bir odadaki katılımı.
/// </summary>
/// <remarks>
/// İlk hâlde bu tabloya <c>RoomUser</c> deniyordu ve <c>int Type</c>,
/// <c>int QueueNum</c>, <c>DateTime? Ping</c> gibi ne olduğu belirsiz alanlar
/// içeriyordu. Adı, katılımcının odadaki <b>rolünü</b> anlatacak şekilde
/// değiştirildi; alanlar anlamlandırıldı.
/// </remarks>
public class RoomParticipant : EntityBase
{
    public Guid RoomId { get; set; }
    public Guid UserId { get; set; }

    public ParticipantRole Role { get; set; } = ParticipantRole.Player;

    /// <summary>Odaya katılım sırası. Beklemede sıralama için kullanılır.</summary>
    public int JoinOrder { get; set; }

    /// <summary>Çok oyunculu odada başlamaya hazır mı?</summary>
    public bool IsReady { get; set; }

    /// <summary>
    /// Son canlılık sinyali. Bağlantısı kopan oyuncuların odayı süresiz
    /// kilitlemesini engellemek için kullanılır (eski adı: <c>Ping</c>).
    /// </summary>
    public DateTime? LastSeenAtUtc { get; set; }

    /// <summary>Bu odadaki toplam puanı (yarışma bittiğinde kesinleşir).</summary>
    public int TotalScore { get; set; }

    public Room Room { get; set; } = null!;
    public User User { get; set; } = null!;

    public ICollection<Competition> Competitions { get; set; } = [];
}
