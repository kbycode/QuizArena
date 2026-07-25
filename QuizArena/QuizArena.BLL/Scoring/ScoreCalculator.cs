using QuizArena.BLL.Constants;
using QuizArena.Entities.Dtos.Play;
using QuizArena.Entities.Enums;

namespace QuizArena.BLL.Scoring;

/// <summary>
/// Puan hesaplama kuralları.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden ayrı ve saf (pure) bir sınıf?</b> Puanlama, oyunun en çok
/// tartışılacak ve en çok değişecek parçasıdır. Veritabanına, saate veya
/// HTTP bağlamına dokunmadığı için:
/// </para>
/// <list type="bullet">
///   <item>Birim testi tek satırla yazılır (bkz. <c>ScoreCalculatorTests</c>).</item>
///   <item>Aynı girdi her zaman aynı çıktıyı verir; "bazen farklı puan verdi" şikâyeti imkânsızdır.</item>
///   <item>Formül değişikliği, oyun akışının hiçbir yerine dokunmadan yapılır.</item>
/// </list>
/// <para>
/// <b>Puan formülü</b>
/// </para>
/// <code>
/// Taban       = zorluk (Kolay 100, Orta 150, Zor 250)
/// Hız bonusu  = Taban × 0,5 × (kalan süre oranı)
/// Seri bonusu = min(mevcut seri, 10) × 10
/// Toplam      = Taban + Hız + Seri     (yanlış/süre dolduysa 0)
/// </code>
/// </remarks>
public static class ScoreCalculator
{
    /// <summary>Yanlış veya süresi dolmuş cevap: hiçbir kalem puan getirmez.</summary>
    public static readonly ScoreBreakdown Zero = new(0, 0, 0, 0);

    /// <summary>
    /// Bir cevabın puanını hesaplar.
    /// </summary>
    /// <param name="difficulty">Sorunun zorluğu.</param>
    /// <param name="isCorrect">Cevap doğru mu?</param>
    /// <param name="elapsedMilliseconds">Sorunun sunulmasından cevaba kadar geçen süre.</param>
    /// <param name="timeLimitSeconds">Soru için tanınan süre.</param>
    /// <param name="currentStreak">
    /// Bu cevaptan <b>önceki</b> ardışık doğru sayısı. Önceki serinin
    /// kullanılması bilinçli: ilk doğru cevap seri bonusu almaz, seri ancak
    /// sürdürüldükçe ödüllendirilir.
    /// </param>
    public static ScoreBreakdown Calculate(
        QuestionDifficulty difficulty,
        bool isCorrect,
        int elapsedMilliseconds,
        int timeLimitSeconds,
        int currentStreak)
    {
        if (!isCorrect)
        {
            return Zero;
        }

        int basePoints = GetBasePoints(difficulty);
        int speedBonus = CalculateSpeedBonus(basePoints, elapsedMilliseconds, timeLimitSeconds);
        int streakBonus = CalculateStreakBonus(currentStreak);

        return new ScoreBreakdown(
            basePoints,
            speedBonus,
            streakBonus,
            basePoints + speedBonus + streakBonus);
    }

    public static int GetBasePoints(QuestionDifficulty difficulty) => difficulty switch
    {
        QuestionDifficulty.Easy => GameRules.EasyBasePoints,
        QuestionDifficulty.Medium => GameRules.MediumBasePoints,
        QuestionDifficulty.Hard => GameRules.HardBasePoints,

        // Veritabanına elle yeni bir zorluk değeri yazılsa bile puan
        // hesabı patlamaz; en düşük tabanla devam eder.
        _ => GameRules.EasyBasePoints
    };

    /// <summary>
    /// Kalan süre oranına göre hız ikramiyesi.
    /// Süre limiti geçersizse (0 veya negatif) ikramiye verilmez —
    /// sıfıra bölme hatası burada yapısal olarak engellenir.
    /// </summary>
    private static int CalculateSpeedBonus(int basePoints, int elapsedMilliseconds, int timeLimitSeconds)
    {
        if (timeLimitSeconds <= 0)
        {
            return 0;
        }

        double limitMilliseconds = timeLimitSeconds * 1_000d;

        // Negatif geçen süre (saat kayması) veya limitin aşılması durumunda
        // oran [0, 1] aralığına kırpılır.
        double usedRatio = Math.Clamp(elapsedMilliseconds / limitMilliseconds, 0d, 1d);
        double remainingRatio = 1d - usedRatio;

        return (int)Math.Round(basePoints * GameRules.MaxSpeedBonusRatio * remainingRatio);
    }

    private static int CalculateStreakBonus(int currentStreak)
    {
        if (currentStreak <= 0)
        {
            return 0;
        }

        return Math.Min(currentStreak, GameRules.MaxStreakForBonus) * GameRules.StreakBonusPerStep;
    }
}
