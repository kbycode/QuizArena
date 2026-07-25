using System.Globalization;
using System.Security.Cryptography;

namespace QuizArena.Core.Utilities.Security.Hashing;

/// <summary>
/// PBKDF2-HMAC-SHA256 tabanlı parola özetleyici.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden projenin ilk hâlindeki HMACSHA512 yaklaşımı değiştirildi?</b>
/// Orijinal kod <c>new HMACSHA512()</c> ile rastgele bir anahtar üretip onu
/// "tuz" olarak kullanıyor, parolayı tek turda özetliyordu. Tuz doğru fikirdi
/// ama tek turluk bir HMAC <b>çok hızlıdır</b>: modern bir GPU saniyede
/// milyarlarca deneme yapar. Parola özetleme fonksiyonunun bilinçli olarak
/// <b>yavaş</b> olması gerekir. PBKDF2 iterasyon sayısıyla bu maliyeti ayarlar.
/// </para>
/// <para>
/// <b>Sabit zamanlı karşılaştırma:</b> orijinal doğrulama bayt bayt dönen bir
/// döngüyle ilk farkta <c>return false</c> yapıyordu. Bu, cevap süresinden
/// "kaç bayt tuttu" bilgisinin sızmasına (timing attack) ve dahası özet
/// uzunlukları farklıysa <c>IndexOutOfRangeException</c>'a açıktı.
/// Burada <see cref="CryptographicOperations.FixedTimeEquals"/> kullanılıyor.
/// </para>
/// <para>
/// Format: <c>pbkdf2-sha256$iterasyon$base64(tuz)$base64(özet)</c> — OWASP'ın
/// önerdiği "self-describing hash" yaklaşımı. Parametreler özetin içinde
/// olduğu için ileride iterasyon artırılsa bile eski kayıtlar doğrulanmaya
/// devam eder; doğrulama <see cref="PasswordVerificationResult.SuccessRehashNeeded"/>
/// döner ve kayıt sessizce güncellenir.
/// </para>
/// </remarks>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string AlgorithmId = "pbkdf2-sha256";
    private const char Separator = '$';

    /// <summary>OWASP 2023 önerisi: PBKDF2-HMAC-SHA256 için en az 600.000 tur.</summary>
    public const int DefaultIterations = 600_000;

    private const int SaltSizeBytes = 16; // 128 bit
    private const int HashSizeBytes = 32; // 256 bit — SHA-256 çıktısıyla aynı

    private static readonly HashAlgorithmName Prf = HashAlgorithmName.SHA256;

    private readonly int _iterations;

    /// <param name="iterations">
    /// Tur sayısı. Üretimde varsayılan bırakılır. Birim testlerinde düşük bir
    /// değer verilerek test süresi kısaltılabilir — algoritmanın davranışı
    /// değişmediği için testin doğrulama gücü azalmaz.
    /// </param>
    public Pbkdf2PasswordHasher(int iterations = DefaultIterations)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);
        _iterations = iterations;
    }

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, _iterations, Prf, HashSizeBytes);

        return string.Join(
            Separator,
            AlgorithmId,
            // InvariantCulture zorunlu: Türkçe gibi kültürlerde sayı biçimi
            // farklı olabilir ve özet başka bir makinede ayrıştırılamaz hâle gelir.
            _iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public PasswordVerificationResult Verify(string password, string encodedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(encodedHash))
        {
            return PasswordVerificationResult.Failed;
        }

        string[] parts = encodedHash.Split(Separator);

        // Bozuk/tanınmayan biçim: doğrulanamaz, ama patlamak da doğru değil.
        if (parts.Length != 4 ||
            !string.Equals(parts[0], AlgorithmId, StringComparison.Ordinal) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int iterations) ||
            iterations <= 0)
        {
            return PasswordVerificationResult.Failed;
        }

        byte[] salt, expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedHash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }

        if (salt.Length == 0 || expectedHash.Length == 0)
        {
            return PasswordVerificationResult.Failed;
        }

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Prf, expectedHash.Length);

        // Sabit zamanlı: uzunluk farkı da dahil hiçbir şey süreden sızmaz.
        if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
        {
            return PasswordVerificationResult.Failed;
        }

        return iterations < _iterations
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Success;
    }
}
