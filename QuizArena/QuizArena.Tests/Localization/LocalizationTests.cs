using System.Globalization;
using QuizArena.BLL.Constants;
using QuizArena.Core.Localization;
using FluentAssertions;
using Xunit;

namespace QuizArena.Tests.Localization;

/// <summary>
/// Çeviri tablolarının bütünlüğü.
/// </summary>
/// <remarks>
/// Eksik bir çeviri sessizce diğer dilin metnini döndürmemeli. Tabloların
/// tutarlılığını <see cref="LocalizedText"/> kurucusu zaten doğruluyor; bu
/// testler o güvenceyi <b>açıkça</b> ifade ediyor ve dil değişiminin gerçekten
/// çalıştığını gösteriyor.
/// </remarks>
public sealed class LocalizationTests : IDisposable
{
    private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

    private static void UseLanguage(string culture)
        => CultureInfo.CurrentUICulture = new CultureInfo(culture);

    [Fact]
    public void Eksik_ceviri_olan_tablo_kurulurken_hata_verir()
    {
        Action act = () => _ = new LocalizedText(
            new Dictionary<string, string> { ["Var"] = "değer", ["Eksik"] = "değer" },
            new Dictionary<string, string> { ["Var"] = "value" });

        act.Should().Throw<InvalidOperationException>().WithMessage("*Eksik*");
    }

    [Theory]
    [InlineData("tr")]
    [InlineData("en")]
    public void Her_iki_dilde_de_tum_mesajlar_cozulur(string culture)
    {
        UseLanguage(culture);

        // Anahtarın kendisi dönüyorsa çeviri bulunamamış demektir.
        Messages.RoomNotFound.Should().NotBe(nameof(Messages.RoomNotFound));
        Messages.InvalidCredentials.Should().NotBe(nameof(Messages.InvalidCredentials));
        Messages.EventCreated.Should().NotBe(nameof(Messages.EventCreated));
        Messages.CompetitionCompleted.Should().NotBe(nameof(Messages.CompetitionCompleted));
    }

    [Fact]
    public void Dil_degisince_mesaj_da_degisir()
    {
        UseLanguage("tr");
        string turkish = Messages.RoomNotFound;

        UseLanguage("en");
        string english = Messages.RoomNotFound;

        turkish.Should().Be("Oda bulunamadı.");
        english.Should().Be("Room not found.");
    }

    [Fact]
    public void Taninmayan_dil_varsayilana_duser()
    {
        // Almanca desteklenmiyor: kullanıcı boş metin değil, varsayılan dili görmeli.
        UseLanguage("de");

        CurrentLanguage.Value.Should().Be(CurrentLanguage.Default);
        Messages.RoomNotFound.Should().Be("Oda bulunamadı.");
    }

    [Fact]
    public void Sinir_iceren_mesajlar_da_cevriliyor()
    {
        UseLanguage("en");
        Messages.NotEnoughQuestionsIn(3).Should().Contain("3 usable questions");

        UseLanguage("tr");
        Messages.NotEnoughQuestionsIn(3).Should().Contain("3 uygun soru");
    }

    public void Dispose() => CultureInfo.CurrentUICulture = _originalUiCulture;
}
