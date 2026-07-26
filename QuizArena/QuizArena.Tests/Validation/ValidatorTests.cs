using QuizArena.BLL.Validation;
using QuizArena.Entities.Dtos.Auth;
using QuizArena.Entities.Dtos.Questions;
using QuizArena.Entities.Dtos.Rooms;
using QuizArena.Entities.Enums;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace QuizArena.Tests.Validation;

/// <summary>Doğrulama kurallarının testleri.</summary>
public sealed class ValidatorTests
{
    // =====================================================================
    //  Kayıt
    // =====================================================================
    private static RegisterRequest ValidRegistration() =>
        new("oyuncu@ornek.test", "Guclu.Parola1", "Ali", "Yılmaz", "AliY");

    [Fact]
    public void Gecerli_kayit_istegi_kabul_edilir()
    {
        ValidationResult result = new RegisterRequestValidator().Validate(ValidRegistration());

        result.IsValid.Should().BeTrue(string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Theory]
    [InlineData("kisa1A", "en az 8 karakter")]
    [InlineData("hepsikucuk1", "büyük harf yok")]
    [InlineData("HEPSIBUYUK1", "küçük harf yok")]
    [InlineData("RakamYokBurada", "rakam yok")]
    public void Zayif_parola_reddedilir(string password, string reason)
    {
        // Politika olmasa "1" gibi bir parolayla kayıt mümkün olurdu.
        ValidationResult result = new RegisterRequestValidator()
            .Validate(ValidRegistration() with { Password = password });

        result.IsValid.Should().BeFalse(reason);
    }

    [Theory]
    [InlineData("gecersiz")]
    [InlineData("bosluk @ornek.test")]
    [InlineData("")]
    public void Gecersiz_eposta_reddedilir(string email)
    {
        ValidationResult result = new RegisterRequestValidator()
            .Validate(ValidRegistration() with { Email = email });

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("ab")]                    // çok kısa
    [InlineData("<script>alert(1)</script>")]  // enjeksiyon denemesi
    [InlineData("boşluk lu")]             // boşluk
    [InlineData("emoji🙂")]                // beklenmeyen karakter kümesi
    public void Gecersiz_takma_ad_reddedilir(string nickname)
    {
        // Takma ad skor tablosunda gösterildiği için karakter kümesi kısıtlı.
        ValidationResult result = new RegisterRequestValidator()
            .Validate(ValidRegistration() with { Nickname = nickname });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Turkce_karakterli_takma_ad_kabul_edilir()
    {
        ValidationResult result = new RegisterRequestValidator()
            .Validate(ValidRegistration() with { Nickname = "Şükrü_Çağrı" });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Giriste_parola_politikasi_uygulanmaz()
    {
        // Politika değişse bile eski parolalarla giriş yapılabilmeli; ayrıca
        // hata mesajı üzerinden parola politikası sızmamalı.
        ValidationResult result = new LoginRequestValidator()
            .Validate(new LoginRequest("oyuncu@ornek.test", "eski"));

        result.IsValid.Should().BeTrue();
    }

    // =====================================================================
    //  Soru
    // =====================================================================
    private static CreateQuestionRequest ValidQuestion(params SaveAnswerRequest[] answers) =>
        new(
            Guid.NewGuid(),
            "Türkiye'nin başkenti hangi şehirdir?",
            QuestionDifficulty.Easy,
            20,
            null,
            answers.Length > 0 ? answers : DefaultAnswers());

    private static SaveAnswerRequest[] DefaultAnswers() =>
    [
        new("Ankara", true, 1),
        new("İstanbul", false, 2),
        new("İzmir", false, 3),
        new("Bursa", false, 4)
    ];

    [Fact]
    public void Gecerli_soru_kabul_edilir()
    {
        ValidationResult result = new CreateQuestionRequestValidator().Validate(ValidQuestion());

        result.IsValid.Should().BeTrue(string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void Dogru_sikki_olmayan_soru_reddedilir()
    {
        // Böyle bir soru yarışmada çıkarsa kimse doğru cevaplayamaz.
        ValidationResult result = new CreateQuestionRequestValidator().Validate(ValidQuestion(
            new SaveAnswerRequest("Ankara", false, 1),
            new SaveAnswerRequest("İstanbul", false, 2)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Iki_dogru_sikki_olan_soru_reddedilir()
    {
        // Aksi hâlde puan, seçilen doğru şıkka göre değişir; oyun adil olmaz.
        ValidationResult result = new CreateQuestionRequestValidator().Validate(ValidQuestion(
            new SaveAnswerRequest("Ankara", true, 1),
            new SaveAnswerRequest("Ankara ili", true, 2),
            new SaveAnswerRequest("İzmir", false, 3)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Tek_sikli_soru_reddedilir()
    {
        ValidationResult result = new CreateQuestionRequestValidator()
            .Validate(ValidQuestion(new SaveAnswerRequest("Ankara", true, 1)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Ayni_metinli_sikler_reddedilir()
    {
        // Oyuncu için çözümsüz bir soru üretir.
        ValidationResult result = new CreateQuestionRequestValidator().Validate(ValidQuestion(
            new SaveAnswerRequest("Ankara", true, 1),
            new SaveAnswerRequest("ankara", false, 2),
            new SaveAnswerRequest("İzmir", false, 3)));

        result.IsValid.Should().BeFalse();
    }

    // =====================================================================
    //  Oda
    // =====================================================================
    [Fact]
    public void Tek_kisilik_odada_oyuncu_sayisi_birden_fazla_olamaz()
    {
        // Aksi hâlde arayüz "başkalarını bekle" durumuna düşer ve oyun başlamaz.
        ValidationResult result = new CreateRoomRequestValidator().Validate(
            new CreateRoomRequest(Guid.NewGuid(), null, RoomMode.Solo, 10, 20, 4));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Cok_oyunculu_odada_en_az_iki_oyuncu_gerekir()
    {
        ValidationResult result = new CreateRoomRequestValidator().Validate(
            new CreateRoomRequest(Guid.NewGuid(), null, RoomMode.PublicMultiplayer, 10, 20, 1));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]     // alt sınırın altında
    [InlineData(500)]   // üst sınırın üstünde
    public void Sinir_disi_soru_sayisi_reddedilir(int questionCount)
    {
        ValidationResult result = new CreateRoomRequestValidator().Validate(
            new CreateRoomRequest(Guid.NewGuid(), null, RoomMode.Solo, questionCount, 20, 1));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Gecerli_oda_istegi_kabul_edilir()
    {
        ValidationResult result = new CreateRoomRequestValidator().Validate(
            new CreateRoomRequest(Guid.NewGuid(), "Akşam turu", RoomMode.PublicMultiplayer, 10, 20, 4));

        result.IsValid.Should().BeTrue(string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Theory]
    [InlineData("ABC")]      // kısa
    [InlineData("ABCDEFG")]  // uzun
    [InlineData("")]
    public void Gecersiz_uzunluktaki_katilim_kodu_reddedilir(string code)
    {
        ValidationResult result = new JoinRoomRequestValidator().Validate(new JoinRoomRequest(code));

        result.IsValid.Should().BeFalse();
    }
}
