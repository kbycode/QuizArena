using System.Collections.Frozen;

namespace QuizArena.Core.Localization;

/// <summary>
/// Anahtar → dile göre metin eşlemesi.
/// </summary>
/// <remarks>
/// <para>
/// İki sözlük alır ve <b>kurucuda</b> ikisinin aynı anahtar kümesine sahip
/// olduğunu doğrular. Eksik bir çeviri böylece ilk istekte sessizce Türkçe
/// metin döndürmek yerine uygulama açılışında patlar — çeviri boşluğu
/// kullanıcıya ulaşmadan görülür.
/// </para>
/// <para>
/// <see cref="FrozenDictionary{TKey,TValue}"/>: sözlükler bir kez kurulup
/// milyonlarca kez okunuyor; donmuş sözlük tam da bu erişim deseni için
/// optimize edilmiş.
/// </para>
/// </remarks>
public sealed class LocalizedText
{
    private readonly FrozenDictionary<string, string> _turkish;
    private readonly FrozenDictionary<string, string> _english;

    public LocalizedText(IReadOnlyDictionary<string, string> turkish, IReadOnlyDictionary<string, string> english)
    {
        EnsureComplete(turkish, english);

        _turkish = turkish.ToFrozenDictionary(StringComparer.Ordinal);
        _english = english.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>Geçerli dildeki metni verir.</summary>
    public string this[string key] =>
        (CurrentLanguage.Value == AppLanguage.English ? _english : _turkish)
            .TryGetValue(key, out string? value)
                ? value
                // Buraya düşülmez: anahtar kümeleri kurucuda doğrulanıyor.
                // Yine de anahtarın kendisini döndürmek, boş metinden iyidir.
                : key;

    private static void EnsureComplete(
        IReadOnlyDictionary<string, string> turkish,
        IReadOnlyDictionary<string, string> english)
    {
        List<string> missingEnglish = turkish.Keys.Where(key => !english.ContainsKey(key)).ToList();
        List<string> missingTurkish = english.Keys.Where(key => !turkish.ContainsKey(key)).ToList();

        if (missingEnglish.Count == 0 && missingTurkish.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "Çeviri tabloları eşleşmiyor. " +
            $"İngilizcede eksik: [{string.Join(", ", missingEnglish)}]. " +
            $"Türkçede eksik: [{string.Join(", ", missingTurkish)}].");
    }
}
