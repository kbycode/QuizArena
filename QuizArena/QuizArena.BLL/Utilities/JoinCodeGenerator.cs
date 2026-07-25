using System.Security.Cryptography;
using QuizArena.BLL.Constants;

namespace QuizArena.BLL.Utilities;

/// <summary>Oda katılım kodu üretici.</summary>
public static class JoinCodeGenerator
{
    /// <summary>
    /// Karıştırılabilir karakterler alfabede yok.
    /// </summary>
    /// <remarks>
    /// <c>0/O</c>, <c>1/I/l</c>, <c>2/Z</c>, <c>5/S</c>, <c>8/B</c> çiftleri
    /// çıkarıldı. Sebep tamamen kullanılabilirlik: kod telefonda okunuyor veya
    /// elle yazılıyor. "Odaya giremiyorum" şikâyetlerinin çoğu yanlış okunan
    /// karakterden kaynaklanır.
    /// </remarks>
    private const string Alphabet = "ACDEFGHJKLMNPQRTUVWXY34679";

    /// <summary>
    /// Kriptografik rastgelelikle kod üretir.
    /// </summary>
    /// <remarks>
    /// <c>Random</c> yerine <see cref="RandomNumberGenerator"/> kullanılıyor:
    /// <c>Random</c> tohumu tahmin edilebilir olduğu için saldırgan üretilen
    /// kodları öngörüp özel odalara sızabilirdi. 26 karakterli alfabeyle
    /// 6 hane ≈ 309 milyon olasılık verir.
    /// </remarks>
    public static string Generate(int length = GameRules.JoinCodeLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 4);

        return RandomNumberGenerator.GetString(Alphabet, length);
    }
}
