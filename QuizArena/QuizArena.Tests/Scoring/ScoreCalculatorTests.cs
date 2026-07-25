using QuizArena.BLL.Constants;
using QuizArena.BLL.Scoring;
using QuizArena.Entities.Dtos.Play;
using QuizArena.Entities.Enums;
using FluentAssertions;
using Xunit;

namespace QuizArena.Tests.Scoring;

/// <summary>
/// Puanlama kurallarının testleri.
/// </summary>
/// <remarks>
/// <see cref="ScoreCalculator"/> saf (pure) bir sınıf olduğu için burada
/// hiçbir sahte nesne (mock), veritabanı veya kurulum yok. Puanlama oyunun en
/// çok tartışılacak parçası; bu testler formülü değiştirmek isteyen birine
/// "neyi bozduğunu" anında söyler.
/// </remarks>
public sealed class ScoreCalculatorTests
{
    [Fact]
    public void Yanlis_cevap_hicbir_puan_getirmez()
    {
        ScoreBreakdown result = ScoreCalculator.Calculate(
            QuestionDifficulty.Hard,
            isCorrect: false,
            elapsedMilliseconds: 500,
            timeLimitSeconds: 20,
            currentStreak: 9);

        result.Should().BeEquivalentTo(ScoreCalculator.Zero);
        result.Total.Should().Be(0);
    }

    [Theory]
    [InlineData(QuestionDifficulty.Easy, GameRules.EasyBasePoints)]
    [InlineData(QuestionDifficulty.Medium, GameRules.MediumBasePoints)]
    [InlineData(QuestionDifficulty.Hard, GameRules.HardBasePoints)]
    public void Taban_puan_zorluga_gore_belirlenir(QuestionDifficulty difficulty, int expectedBasePoints)
    {
        ScoreBreakdown result = ScoreCalculator.Calculate(
            difficulty,
            isCorrect: true,
            // Sürenin tamamı kullanıldı → hız bonusu yok, taban puan yalın görünür.
            elapsedMilliseconds: 20_000,
            timeLimitSeconds: 20,
            currentStreak: 0);

        result.BasePoints.Should().Be(expectedBasePoints);
        result.SpeedBonus.Should().Be(0);
        result.Total.Should().Be(expectedBasePoints);
    }

    [Fact]
    public void Aninda_verilen_cevap_azami_hiz_bonusu_alir()
    {
        ScoreBreakdown result = ScoreCalculator.Calculate(
            QuestionDifficulty.Medium,
            isCorrect: true,
            elapsedMilliseconds: 0,
            timeLimitSeconds: 20,
            currentStreak: 0);

        int expectedMaxBonus = (int)(GameRules.MediumBasePoints * GameRules.MaxSpeedBonusRatio);

        result.SpeedBonus.Should().Be(expectedMaxBonus);
        result.Total.Should().Be(GameRules.MediumBasePoints + expectedMaxBonus);
    }

    [Fact]
    public void Surenin_yarisinda_verilen_cevap_hiz_bonusunun_yarisini_alir()
    {
        ScoreBreakdown result = ScoreCalculator.Calculate(
            QuestionDifficulty.Easy,
            isCorrect: true,
            elapsedMilliseconds: 10_000,
            timeLimitSeconds: 20,
            currentStreak: 0);

        // 100 × 0,5 × 0,5 = 25
        result.SpeedBonus.Should().Be(25);
    }

    [Fact]
    public void Sure_asilsa_bile_hiz_bonusu_negatif_olmaz()
    {
        // Ağ gecikmesi toleransı nedeniyle süre limitini aşan ama yine de
        // doğru sayılan bir cevap mümkün. Bonus sıfıra kırpılmalı.
        ScoreBreakdown result = ScoreCalculator.Calculate(
            QuestionDifficulty.Hard,
            isCorrect: true,
            elapsedMilliseconds: 45_000,
            timeLimitSeconds: 20,
            currentStreak: 0);

        result.SpeedBonus.Should().Be(0);
        result.Total.Should().Be(GameRules.HardBasePoints);
    }

    [Fact]
    public void Negatif_gecen_sure_bonusu_sismez()
    {
        // Sunucu saatinin geriye kayması gibi uç bir durumda geçen süre
        // negatif hesaplanabilir; bu, azami bonustan fazlasını vermemeli.
        ScoreBreakdown result = ScoreCalculator.Calculate(
            QuestionDifficulty.Easy,
            isCorrect: true,
            elapsedMilliseconds: -5_000,
            timeLimitSeconds: 20,
            currentStreak: 0);

        result.SpeedBonus.Should().Be((int)(GameRules.EasyBasePoints * GameRules.MaxSpeedBonusRatio));
    }

    [Fact]
    public void Sifir_sure_limiti_sifira_bolme_hatasi_uretmez()
    {
        Action act = () => ScoreCalculator.Calculate(
            QuestionDifficulty.Easy,
            isCorrect: true,
            elapsedMilliseconds: 1_000,
            timeLimitSeconds: 0,
            currentStreak: 0);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, GameRules.StreakBonusPerStep)]
    [InlineData(5, 5 * GameRules.StreakBonusPerStep)]
    public void Seri_bonusu_ardisik_dogru_sayisiyla_artar(int streak, int expectedBonus)
    {
        ScoreBreakdown result = ScoreCalculator.Calculate(
            QuestionDifficulty.Easy,
            isCorrect: true,
            elapsedMilliseconds: 15_000,
            timeLimitSeconds: 15,
            currentStreak: streak);

        result.StreakBonus.Should().Be(expectedBonus);
    }

    [Fact]
    public void Seri_bonusu_ust_sinirda_durur()
    {
        // 50 doğru serisi de 10 doğru serisi de aynı bonusu alır:
        // uzun serilerin puanı dengesiz biçimde şişirmesi engellenir.
        ScoreBreakdown atLimit = ScoreCalculator.Calculate(
            QuestionDifficulty.Easy, true, 15_000, 15, GameRules.MaxStreakForBonus);

        ScoreBreakdown farAboveLimit = ScoreCalculator.Calculate(
            QuestionDifficulty.Easy, true, 15_000, 15, GameRules.MaxStreakForBonus * 5);

        farAboveLimit.StreakBonus.Should().Be(atLimit.StreakBonus);
        atLimit.StreakBonus.Should().Be(GameRules.MaxStreakForBonus * GameRules.StreakBonusPerStep);
    }

    [Fact]
    public void Toplam_puan_kalemlerin_toplamina_esittir()
    {
        ScoreBreakdown result = ScoreCalculator.Calculate(
            QuestionDifficulty.Hard,
            isCorrect: true,
            elapsedMilliseconds: 4_000,
            timeLimitSeconds: 20,
            currentStreak: 3);

        result.Total.Should().Be(result.BasePoints + result.SpeedBonus + result.StreakBonus);
    }

    [Fact]
    public void Tanimsiz_zorluk_degeri_hata_yerine_en_dusuk_tabani_kullanir()
    {
        // Veritabanına elle geçersiz bir zorluk değeri yazılsa bile
        // puanlama çökmemeli.
        ScoreBreakdown result = ScoreCalculator.Calculate(
            (QuestionDifficulty)99,
            isCorrect: true,
            elapsedMilliseconds: 10_000,
            timeLimitSeconds: 10,
            currentStreak: 0);

        result.BasePoints.Should().Be(GameRules.EasyBasePoints);
    }
}
