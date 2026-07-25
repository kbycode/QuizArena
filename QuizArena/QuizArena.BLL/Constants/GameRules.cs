namespace QuizArena.BLL.Constants;

/// <summary>
/// Oyunun sayısal kuralları tek yerde.
/// </summary>
/// <remarks>
/// Bu değerler kod içine dağılmış "sihirli sayılar" olarak durmuyor; hem
/// doğrulama (validator) hem puanlama hem de arayüz sınırları aynı kaynağı
/// okuyor. Böylece "validator 50 soruya izin veriyor ama oyun 20'den fazlasını
/// kaldıramıyor" türü tutarsızlıklar oluşmuyor.
/// </remarks>
public static class GameRules
{
    // --- Oda sınırları -------------------------------------------------------
    public const int MinQuestionCount = 5;
    public const int MaxQuestionCount = 30;
    public const int DefaultQuestionCount = 10;

    public const int MinSecondsPerQuestion = 5;
    public const int MaxSecondsPerQuestion = 60;
    public const int DefaultSecondsPerQuestion = 20;

    public const int MinPlayers = 1;
    public const int MaxPlayers = 8;

    public const int JoinCodeLength = 6;

    // --- Puanlama ------------------------------------------------------------
    public const int EasyBasePoints = 100;
    public const int MediumBasePoints = 150;
    public const int HardBasePoints = 250;

    /// <summary>
    /// Hız ikramiyesinin taban puana oranı üst sınırı.
    /// Soruyu anında bilen oyuncu taban puanın %50'si kadar ek puan alır.
    /// </summary>
    public const double MaxSpeedBonusRatio = 0.5;

    /// <summary>Ardışık her doğru cevap için eklenen puan.</summary>
    public const int StreakBonusPerStep = 10;

    /// <summary>Seri ikramiyesinin sayıldığı üst sınır (dengesizliği önler).</summary>
    public const int MaxStreakForBonus = 10;

    // --- Zamanlama toleransı -------------------------------------------------
    /// <summary>
    /// Cevap süresine tanınan tolerans.
    /// </summary>
    /// <remarks>
    /// Sunucu süreyi kendisi ölçer; ancak isteğin ağ üzerinde geçirdiği süre
    /// oyuncunun kusuru değildir. Bu tolerans olmadan, son saniyede verilen
    /// doğru cevaplar mobil bağlantılarda haksız yere "süre doldu" sayılırdı.
    /// Tolerans, hile için anlamlı bir avantaj sağlamayacak kadar kısa tutulmuştur.
    /// </remarks>
    public const int TimingToleranceMilliseconds = 1_500;

    // --- Rozet eşikleri ------------------------------------------------------
    public const int QuickThinkerThresholdMilliseconds = 3_000;
    public const int VeteranCompetitionCount = 10;
    public const int StreakMasterThreshold = 10;

    // --- Sıralama tablosu ----------------------------------------------------
    public const int LeaderboardDefaultTop = 20;
    public const int LeaderboardMaxTop = 100;
    public const int LeaderboardCacheMinutes = 2;
    public const int CategoryCacheMinutes = 15;
}
