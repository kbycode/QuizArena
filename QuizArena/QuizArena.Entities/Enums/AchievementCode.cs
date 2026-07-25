namespace QuizArena.Entities.Enums;

/// <summary>
/// Rozet (başarım) kodları. Rozet <b>metinleri</b> veritabanında durur,
/// <b>kazanma koşulu</b> ise koddadır; bu enum ikisini birbirine bağlar.
/// </summary>
public enum AchievementCode
{
    /// <summary>İlk yarışmayı tamamla.</summary>
    FirstBlood = 1,

    /// <summary>Bir yarışmada tüm soruları doğru cevapla.</summary>
    Perfectionist = 2,

    /// <summary>Bir soruyu 3 saniyenin altında doğru cevapla.</summary>
    QuickThinker = 3,

    /// <summary>10 yarışma tamamla.</summary>
    Veteran = 4,

    /// <summary>Üst üste 10 soruyu doğru cevapla.</summary>
    StreakMaster = 5,

    /// <summary>Çok oyunculu bir yarışmayı birinci bitir.</summary>
    Champion = 6
}
