using QuizArena.Core.Exceptions;
using QuizArena.Core.Utilities.Security;

namespace QuizArena.Tests.Infrastructure;

/// <summary>
/// Testlerde "istek yapan kullanıcı"yı taklit eder.
/// </summary>
/// <remarks>
/// <see cref="ICurrentUserService"/> soyutlamasının bedeli bu 30 satır;
/// karşılığında iş katmanı testlerinde sahte bir <c>HttpContext</c>,
/// <c>ClaimsPrincipal</c> ve <c>IHttpContextAccessor</c> zinciri kurmak
/// gerekmiyor. Kullanıcı değiştirmek tek satır: <c>SetUser(id)</c>.
/// </remarks>
public sealed class FakeCurrentUserService : ICurrentUserService
{
    private readonly HashSet<string> _roles = new(StringComparer.Ordinal);

    public Guid? UserId { get; private set; }

    public string? Email { get; private set; }

    public bool IsAuthenticated => UserId is not null;

    public IReadOnlyCollection<string> Roles => _roles;

    public string? IpAddress => "127.0.0.1";

    public void SetUser(Guid userId, string? email = null, params string[] roles)
    {
        UserId = userId;
        Email = email;

        _roles.Clear();
        foreach (string role in roles)
        {
            _roles.Add(role);
        }
    }

    public void SignOut()
    {
        UserId = null;
        Email = null;
        _roles.Clear();
    }

    public bool IsInRole(string role) => _roles.Contains(role);

    public Guid RequireUserId() =>
        UserId ?? throw new UnauthorizedException("Test: oturum açmış kullanıcı ayarlanmadı.");
}
