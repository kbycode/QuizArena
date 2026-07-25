namespace QuizArena.Core.Utilities.Security.Jwt;

/// <summary>Kimlik doğrulama sonucunda üretilen jeton çifti.</summary>
public sealed class AccessToken
{
    public string Token { get; init; } = null!;

    public DateTime ExpiresAtUtc { get; init; }

    /// <summary>Ham yenileme jetonu. Yalnızca burada, bir kez istemciye döner.</summary>
    public string RefreshToken { get; init; } = null!;

    public DateTime RefreshTokenExpiresAtUtc { get; init; }
}
