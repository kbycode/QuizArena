using System.Security.Claims;
using QuizArena.Core.Exceptions;
using Microsoft.AspNetCore.Http;

namespace QuizArena.Core.Utilities.Security;

/// <inheritdoc cref="ICurrentUserService"/>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            string? raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out Guid id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? [];

    /// <summary>
    /// İstemci IP'si. Ters vekil (reverse proxy) arkasında doğru değeri
    /// alabilmek için <c>ForwardedHeaders</c> middleware'i açık olmalıdır;
    /// aksi hâlde burada proxy'nin IP'si görünür. Bu yüzden başlığı burada
    /// elle okumuyoruz — <c>X-Forwarded-For</c>'a körlemesine güvenmek
    /// IP'nin istemci tarafından uydurulmasına (spoofing) izin verir.
    /// </summary>
    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;

    public Guid RequireUserId() =>
        UserId ?? throw new UnauthorizedException("Bu işlem için oturum açmanız gerekiyor.");
}
