using QuizArena.Core.Entities;
using QuizArena.Core.Entities.Concrete;

namespace QuizArena.Entities.Concrete;

/// <summary>
/// Kullanıcının birikimli istatistikleri.
/// </summary>
/// <remarks>
/// <b>Neden ayrı tablo ve neden önceden hesaplanmış (denormalize)?</b>
/// Sıralama tablosu her açıldığında milyonlarca cevap satırını
/// <c>SUM</c>/<c>COUNT</c> ile taramak, kullanıcı sayısı arttıkça
/// sürdürülemez. Bu satır yarışma bittiğinde bir kez güncellenir
/// (tek <c>UPDATE</c>), sıralama ise tek indeksli okumaya iner.
/// Doğruluk kaynağı yine cevap tabloları olduğu için istatistik gerekirse
/// yeniden hesaplanabilir.
/// </remarks>
public class UserStatistic : EntityBase
{
    public Guid UserId { get; set; }

    public int TotalCompetitions { get; set; }
    public int TotalQuestionsAnswered { get; set; }
    public int TotalCorrectAnswers { get; set; }
    public int TotalScore { get; set; }

    /// <summary>Tüm zamanların en uzun ardışık doğru serisi.</summary>
    public int BestStreak { get; set; }

    /// <summary>Tek bir yarışmadan alınan en yüksek puan.</summary>
    public int BestCompetitionScore { get; set; }

    /// <summary>Çok oyunculu yarışmalarda birincilik sayısı.</summary>
    public int WinCount { get; set; }

    /// <summary>Ortalama cevap süresi (ms). Yeni cevaplarla hareketli olarak güncellenir.</summary>
    public int AverageAnswerMilliseconds { get; set; }

    public DateTime? LastPlayedAtUtc { get; set; }

    public User User { get; set; } = null!;

    public double AccuracyPercentage =>
        TotalQuestionsAnswered == 0
            ? 0
            : Math.Round(TotalCorrectAnswers * 100d / TotalQuestionsAnswered, 1);
}
