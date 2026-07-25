using QuizArena.Core.DataAccess;
using QuizArena.Core.Entities.Concrete;

namespace QuizArena.DAL.Abstract;

/// <summary>
/// Kullanıcı deposu. Jenerik CRUD'un üstüne, yalnızca SQL'e çevrilebilir
/// biçimde yazılması anlamlı olan sorgular eklenir.
/// </summary>
public interface IUserRepository : IEntityRepository<User>
{
    Task<User?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının yetki adlarını döner.
    /// </summary>
    /// <remarks>
    /// Yalnızca <c>Name</c> kolonunu seçer. İlk hâldeki sürüm
    /// <c>OperationClaim</c> nesnelerinin tamamını materyalize ediyordu;
    /// JWT'ye yazılacak olan ise sadece isimdi.
    /// </remarks>
    Task<IReadOnlyList<string>> GetOperationClaimNamesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public interface IOperationClaimRepository : IEntityRepository<OperationClaim>
{
    Task<OperationClaim?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}

public interface IUserOperationClaimRepository : IEntityRepository<UserOperationClaim>;

public interface IRefreshTokenRepository : IEntityRepository<RefreshToken>
{
    /// <summary>Ham jetonun özetiyle kaydı bulur (kullanıcı bilgisiyle birlikte).</summary>
    Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının tüm aktif jetonlarını iptal eder.
    /// Jeton yeniden kullanımı (token replay) tespit edildiğinde ve parola
    /// değiştiğinde çağrılır: tüm oturumlar düşer.
    /// </summary>
    Task RevokeAllActiveAsync(
        Guid userId,
        string reason,
        string? ipAddress,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
