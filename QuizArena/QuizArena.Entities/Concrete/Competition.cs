using QuizArena.Core.Entities;
using QuizArena.Entities.Enums;

namespace QuizArena.Entities.Concrete;

/// <summary>
/// Bir katılımcının bir odadaki yarışma oturumu (kendi soru seti ve skoru).
/// </summary>
/// <remarks>
/// <para>
/// Modelin özü: <b>oda</b> ortak ayarları tutar, <b>yarışma</b> ise
/// katılımcıya özeldir. Böylece çok oyunculu bir odada her oyuncunun
/// kendi ilerlemesi, kendi süresi ve kendi puanı ayrı ayrı izlenebilir;
/// hepsi aynı soru sırasını görür ama biri diğerini beklemek zorunda kalmaz.
/// </para>
/// <para>
/// İlk hâlde <c>Competition</c> yalnızca <c>RoomUserId</c> ve süre alanları
/// içeriyordu; durum (bitti mi?), puan ve doğru sayısı yoktu — yani yarışmanın
/// sonucu hiçbir yerde tutulmuyordu.
/// </para>
/// </remarks>
public class Competition : EntityBase
{
    public Guid RoomId { get; set; }
    public Guid RoomParticipantId { get; set; }

    public CompetitionStatus Status { get; set; } = CompetitionStatus.InProgress;

    public int QuestionCount { get; set; }

    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    // --- Sonuç alanları (yarışma bitince hesaplanır) ------------------------
    public int TotalScore { get; set; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }

    /// <summary>Süresi dolduğu için cevaplanamayan soru sayısı.</summary>
    public int TimedOutCount { get; set; }

    /// <summary>
    /// Şu anda devam eden ardışık doğru serisi.
    /// </summary>
    /// <remarks>
    /// Kolon olarak tutulmasının nedeni performans: alternatifi, her cevapta
    /// o yarışmanın tüm cevaplarını çekip sondan geriye sayarak seriyi
    /// hesaplamaktı. Bu, soru başına fazladan bir sorgu ve giderek büyüyen
    /// bir veri kümesi anlamına gelir. Seri bilgisi cevapla birlikte aynı
    /// işlemde güncellendiği için tutarsızlık riski yoktur.
    /// </remarks>
    public int CurrentStreak { get; set; }

    /// <summary>Bu oturumdaki en uzun ardışık doğru serisi.</summary>
    public int LongestStreak { get; set; }

    public Room Room { get; set; } = null!;
    public RoomParticipant RoomParticipant { get; set; } = null!;

    public ICollection<CompetitionQuestion> CompetitionQuestions { get; set; } = [];

    /// <summary>Sonuçlanmış soru sayısı (süresi dolanlar dahil).</summary>
    public int AnsweredCount => CorrectCount + WrongCount + TimedOutCount;

    /// <summary>Doğruluk oranı (0-100). Sıfıra bölme burada bir kez engellenir.</summary>
    public double AccuracyPercentage =>
        QuestionCount == 0 ? 0 : Math.Round(CorrectCount * 100d / QuestionCount, 1);
}
