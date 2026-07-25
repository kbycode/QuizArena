using QuizArena.Core.DataAccess.EntityFramework;
using QuizArena.Core.Entities.Concrete;
using QuizArena.DAL.Abstract;
using QuizArena.DAL.Contexts;
using Microsoft.EntityFrameworkCore;

namespace QuizArena.DAL.Concrete.EntityFramework;

public sealed class EfUserRepository
    : EfEntityRepositoryBase<User, QuizArenaDbContext>, IUserRepository
{
    public EfUserRepository(QuizArenaDbContext context) : base(context) { }

    public Task<User?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
        => GetAsync(u => u.NormalizedEmail == normalizedEmail, asNoTracking: asNoTracking,
            cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<string>> GetOperationClaimNamesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await Context.UserOperationClaims
            .AsNoTracking()
            .Where(uoc => uoc.UserId == userId)
            .Select(uoc => uoc.OperationClaim.Name)
            .ToListAsync(cancellationToken);
}

public sealed class EfOperationClaimRepository
    : EfEntityRepositoryBase<OperationClaim, QuizArenaDbContext>, IOperationClaimRepository
{
    public EfOperationClaimRepository(QuizArenaDbContext context) : base(context) { }

    public Task<OperationClaim?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => GetAsync(oc => oc.Name == name, cancellationToken: cancellationToken);
}

public sealed class EfUserOperationClaimRepository
    : EfEntityRepositoryBase<UserOperationClaim, QuizArenaDbContext>, IUserOperationClaimRepository
{
    public EfUserOperationClaimRepository(QuizArenaDbContext context) : base(context) { }
}

public sealed class EfRefreshTokenRepository
    : EfEntityRepositoryBase<RefreshToken, QuizArenaDbContext>, IRefreshTokenRepository
{
    public EfRefreshTokenRepository(QuizArenaDbContext context) : base(context) { }

    public Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
        => Context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

    /// <remarks>
    /// Tek <c>UPDATE</c> cümlesi (<c>ExecuteUpdateAsync</c>) çalıştırır.
    /// Alternatifi, tüm jetonları belleğe çekip döngüyle güncellemekti;
    /// yüzlerce jetonu olan bir kullanıcıda bu yüzlerce satırlık ağ trafiği
    /// ve gereksiz bellek anlamına gelir.
    /// </remarks>
    public Task RevokeAllActiveAsync(
        Guid userId,
        string reason,
        string? ipAddress,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
        => Context.RefreshTokens
            .Where(rt => rt.UserId == userId
                         && rt.RevokedAtUtc == null
                         && rt.ExpiresAtUtc > utcNow)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(rt => rt.RevokedAtUtc, utcNow)
                    .SetProperty(rt => rt.RevokeReason, reason)
                    .SetProperty(rt => rt.RevokedByIp, ipAddress)
                    .SetProperty(rt => rt.UpdatedAtUtc, utcNow),
                cancellationToken);
}
