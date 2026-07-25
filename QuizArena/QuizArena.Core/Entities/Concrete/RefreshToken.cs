namespace QuizArena.Core.Entities.Concrete;

/// <summary>
/// Yenileme (refresh) jetonu kaydı.
/// </summary>
/// <remarks>
/// <para>
/// <b>Jetonun kendisi saklanmaz, SHA-256 özeti saklanır.</b> Sebebi: refresh
/// token pratikte bir paroladır. Veritabanı sızarsa düz metin jetonlarla
/// saldırgan tüm kullanıcıların oturumunu devralır; özet saklandığında
/// ele geçirilen veri işe yaramaz.
/// </para>
/// <para>
/// <b>Rotasyon + yeniden kullanım tespiti:</b> jeton her kullanıldığında iptal
/// edilip yerine yenisi verilir (<see cref="ReplacedByTokenHash"/>). Zaten
/// iptal edilmiş bir jeton tekrar kullanılırsa bu "jeton çalınmış" sinyalidir;
/// o kullanıcının tüm jeton zinciri toptan iptal edilir.
/// </para>
/// </remarks>
public class RefreshToken : EntityBase
{
    public Guid UserId { get; set; }

    /// <summary>Jetonun SHA-256 özeti (Base64). Düz metin asla saklanmaz.</summary>
    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAtUtc { get; set; }

    public string? CreatedByIp { get; set; }

    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokedByIp { get; set; }
    public string? RevokeReason { get; set; }

    /// <summary>Rotasyonda bu jetonun yerine geçen jetonun özeti.</summary>
    public string? ReplacedByTokenHash { get; set; }

    public User User { get; set; } = null!;

    public bool IsRevoked => RevokedAtUtc is not null;

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;

    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);
}
