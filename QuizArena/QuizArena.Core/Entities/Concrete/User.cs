namespace QuizArena.Core.Entities.Concrete;

/// <summary>
/// Kimlik doğrulanan kullanıcı. Kimlik/yetki Core'a ait genel bir kavram
/// olduğu için burada durur, <c>Entities</c> katmanında değil.
/// </summary>
public class User : EntityBase
{
    public string Email { get; set; } = null!;

    /// <summary>
    /// Aramada ve tekillik kontrolünde kullanılan normalize (büyük harf,
    /// kültürden bağımsız) e-posta.
    /// <para>
    /// Neden ayrı kolon: veritabanı collation'ına güvenerek
    /// <c>WHERE Email = @e</c> yazmak taşınabilir değildir; ayrıca
    /// <c>UPPER(Email)</c> gibi bir çağrı tekil indeksin kullanılmasını
    /// engeller (SARGability kaybı). Türkçe'ye özgü "i/İ" sorunu da
    /// <c>ToUpperInvariant</c> ile burada kesin olarak çözülür.
    /// </para>
    /// </summary>
    public string NormalizedEmail { get; set; } = null!;

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;

    /// <summary>Yarışma ekranlarında ve sıralama tablosunda görünen ad.</summary>
    public string Nickname { get; set; } = null!;

    public string? AvatarUrl { get; set; }
    public string? City { get; set; }
    public DateOnly? BirthDate { get; set; }

    /// <summary>
    /// Parolanın kendi kendini tanımlayan (self-describing) özeti:
    /// <c>pbkdf2-sha256$iterasyon$tuz$özet</c>.
    /// <para>
    /// Tuz ve algoritma parametreleri özetin içine gömülü olduğu için
    /// yıllar sonra iterasyon sayısı artırıldığında eski kayıtlar da
    /// doğrulanmaya devam eder; ayrı <c>PasswordSalt</c> kolonu gerekmez.
    /// </para>
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// Parola/yetki değiştiğinde yenilenen damga. Dağıtılmış JWT'lerin
    /// erken geçersiz kılınmasını sağlar: token içindeki damga ile
    /// veritabanındaki damga uyuşmuyorsa oturum düşer.
    /// </summary>
    public string SecurityStamp { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    // --- Kaba kuvvet (brute force) koruması ---------------------------------
    public int AccessFailedCount { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }

    public ICollection<UserOperationClaim> UserOperationClaims { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}";
}
