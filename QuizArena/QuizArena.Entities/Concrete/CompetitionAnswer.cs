using QuizArena.Core.Entities;

namespace QuizArena.Entities.Concrete;

/// <summary>
/// Oyuncunun bir yarışma sorusuna verdiği cevap ve o cevabın sonucu.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CompetitionQuestionId"/> üzerinde <b>tekil (unique)</b> indeks
/// vardır. Bu, "aynı soruya ikinci kez cevap gönderip puanı katlama"
/// saldırısını uygulama kodundaki kontrole değil <b>veritabanı kısıtına</b>
/// bağlar. İki isteğin aynı milisaniyede gelmesi (race condition) durumunda
/// bile ikincisi veritabanı seviyesinde reddedilir.
/// </para>
/// <para>
/// (İlk hâlindeki adı <c>CompetitionQuestionAnswer</c> idi ve yalnızca iki
/// kimlik alanı içeriyordu: doğruluk, puan ve süre bilgisi hiç tutulmuyordu.)
/// </para>
/// </remarks>
public class CompetitionAnswer : EntityBase
{
    public Guid CompetitionQuestionId { get; set; }

    /// <summary>
    /// Seçilen şık. Süre dolduğu için cevap verilemediyse <c>null</c>
    /// (bu durumda kayıt yine oluşturulur: "cevaplanmadı" da bir sonuçtur).
    /// </summary>
    public Guid? SelectedAnswerId { get; set; }

    public DateTime AnsweredAtUtc { get; set; }

    /// <summary>Soru servis edildiğinden cevaba kadar geçen süre — sunucuda ölçülür.</summary>
    public int ElapsedMilliseconds { get; set; }

    public bool IsCorrect { get; set; }

    /// <summary>Süre dolduğu için mi kapandı?</summary>
    public bool IsTimedOut { get; set; }

    /// <summary>Temel puan (zorluğa göre).</summary>
    public int BasePoints { get; set; }

    /// <summary>Hız ikramiyesi.</summary>
    public int SpeedBonus { get; set; }

    /// <summary>Ardışık doğru serisi ikramiyesi.</summary>
    public int StreakBonus { get; set; }

    /// <summary>Bu cevaptan kazanılan toplam puan.</summary>
    public int EarnedPoints { get; set; }

    public CompetitionQuestion CompetitionQuestion { get; set; } = null!;

    public Answer? SelectedAnswer { get; set; }
}
