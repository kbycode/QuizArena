namespace QuizArena.Api.Configuration;

/// <summary>Hız sınırlama (rate limiting) ayarları.</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Genel uçlar için dakikada izin verilen istek sayısı.</summary>
    public int GeneralPermitPerMinute { get; set; } = 120;

    /// <summary>
    /// Kimlik doğrulama uçları için dakikada izin verilen istek sayısı.
    /// </summary>
    /// <remarks>
    /// Ayrı ve çok daha düşük tutulur. Sebebi: <c>/api/auth/login</c> ucu
    /// parola deneme saldırılarının hedefidir. Hesap bazlı kilitleme
    /// (bkz. <c>AuthManager</c>) tek bir hesabı korur; hız sınırı ise
    /// "bin farklı hesaba birer deneme" (credential stuffing) biçimindeki
    /// dağıtık saldırıyı da yavaşlatır.
    /// </remarks>
    public int AuthPermitPerMinute { get; set; } = 10;
}
