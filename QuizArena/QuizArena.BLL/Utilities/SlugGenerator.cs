using System.Text;

namespace QuizArena.BLL.Utilities;

/// <summary>Metinden URL'de kullanılabilir kısa ad (slug) üretir.</summary>
public static class SlugGenerator
{
    /// <summary>
    /// Türkçe karakterlerin ASCII karşılıkları.
    /// </summary>
    /// <remarks>
    /// <c>string.Normalize(NormalizationForm.FormD)</c> ile aksan ayıklama
    /// yaklaşımı Türkçe için <b>doğru sonuç vermez</b>: <c>ı</c> harfi
    /// <c>i</c>'nin aksanlı hâli değil, ayrı bir harftir; <c>ş</c> ve <c>ç</c>
    /// ise ayrıştığında noktalama olarak elenir. Bu yüzden eşleme elle
    /// tanımlanmıştır.
    /// </remarks>
    private static readonly Dictionary<char, string> TurkishMap = new()
    {
        ['ç'] = "c", ['Ç'] = "c",
        ['ğ'] = "g", ['Ğ'] = "g",
        ['ı'] = "i", ['I'] = "i",
        ['İ'] = "i", ['i'] = "i",
        ['ö'] = "o", ['Ö'] = "o",
        ['ş'] = "s", ['Ş'] = "s",
        ['ü'] = "u", ['Ü'] = "u"
    };

    public static string Generate(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var builder = new StringBuilder(text.Length);
        var previousWasSeparator = false;

        foreach (char character in text.Trim())
        {
            if (TurkishMap.TryGetValue(character, out string? replacement))
            {
                builder.Append(replacement);
                previousWasSeparator = false;
                continue;
            }

            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
                continue;
            }

            // Birden fazla boşluk/noktalama tek tire olur; baştaki atlanır.
            if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
