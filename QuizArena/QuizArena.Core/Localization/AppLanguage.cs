using System.Globalization;

namespace QuizArena.Core.Localization;

/// <summary>
/// Uygulamanın desteklediği diller ve o an geçerli olanın çözümü.
/// </summary>
/// <remarks>
/// <para>
/// Dil, isteğin <c>Accept-Language</c> başlığından belirlenir; ASP.NET Core'un
/// istek yerelleştirme ara katmanı bunu <see cref="CultureInfo.CurrentUICulture"/>
/// içine yazar. Buradaki iş yalnızca o kültürü iki desteklenen dilden birine
/// indirgemek.
/// </para>
/// <para>
/// <b>Neden <c>.resx</c> değil?</b> Kaynak dosyaları tasarım zamanında üretilen
/// sınıflara ve uydu derlemelerine (satellite assembly) bağlı. İki dil ve birkaç
/// yüz metin için bu, kazandırdığından fazla kurulum getiriyor. Sözlük tabanlı
/// çözüm derlemede tek dosya, testte kolayca doğrulanabilir ve eksik anahtar
/// hatasını çalışma anında değil <b>başlangıçta</b> yakalanabilir kılıyor
/// (bkz. <c>LocalizedText.EnsureComplete</c>).
/// </para>
/// </remarks>
public enum AppLanguage
{
    Turkish = 0,
    English = 1
}

/// <summary>Geçerli dili çözer.</summary>
public static class CurrentLanguage
{
    /// <summary>Desteklenen kültür kodları; istek yerelleştirmesi de bunu kullanır.</summary>
    public static readonly string[] SupportedCultures = ["tr", "en"];

    /// <summary>
    /// Varsayılan dil.
    /// </summary>
    /// <remarks>
    /// Tanınmayan bir <c>Accept-Language</c> geldiğinde Türkçe'ye düşülür:
    /// içerik ve demo verisi öncelikli olarak Türkçe hazırlanıyor.
    /// </remarks>
    public const AppLanguage Default = AppLanguage.Turkish;

    /// <summary>O anki isteğin dili.</summary>
    public static AppLanguage Value =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.English
            : Default;
}
