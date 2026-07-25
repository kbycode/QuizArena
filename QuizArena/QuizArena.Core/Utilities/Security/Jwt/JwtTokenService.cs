using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using QuizArena.Core.Entities.Concrete;
using QuizArena.Core.Utilities.Clock;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace QuizArena.Core.Utilities.Security.Jwt;

/// <inheritdoc cref="ITokenService"/>
/// <remarks>
/// Orijinal <c>JwtHelper</c>'a göre düzeltilen noktalar:
/// <list type="bullet">
///   <item>
///     <b><c>DateTime.Now</c> → <c>UtcNow</c>:</b> JWT'nin <c>exp</c>/<c>nbf</c>
///     alanları tanım gereği UTC epoch'tur. Yerel saatle üretilen jeton, UTC+3
///     bir sunucuda "3 saat sonra geçerli olacak" (<c>nbf</c> gelecekte) diye
///     reddedilir; UTC-5 bir sunucuda da beklenenden 5 saat fazla yaşar.
///   </item>
///   <item>
///     <b>Ömür hesabı artık kurucuda (constructor) değil:</b> orijinalde
///     <c>_accessTokenExpiration</c> nesne kurulurken bir kez hesaplanıyordu.
///     Servis singleton olarak kaydedilse (veya uzun ömürlü kalsa) üretilen tüm
///     jetonlar aynı, giderek geçmişe kayan bitiş tarihini taşırdı.
///   </item>
///   <item>
///     <b><c>IOptions</c> ile ayar:</b> orijinal doğrudan <c>IConfiguration</c>
///     okuyup <c>null</c> kontrolü yapmıyordu; ayar eksikse
///     <c>NullReferenceException</c> geliyordu.
///   </item>
/// </list>
/// </remarks>
public sealed class JwtTokenService : ITokenService
{
    private const int RefreshTokenSizeBytes = 32; // 256 bit

    private readonly TokenOptions _options;
    private readonly IClock _clock;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(IOptions<TokenOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecurityKey));
        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    }

    public AccessToken CreateAccessToken(User user, IEnumerable<string> operationClaims)
    {
        ArgumentNullException.ThrowIfNull(user);

        DateTime now = _clock.UtcNow;
        DateTime accessExpiration = now.AddMinutes(_options.AccessTokenExpirationMinutes);

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: BuildClaims(user, operationClaims),
            notBefore: now,
            expires: accessExpiration,
            signingCredentials: _signingCredentials);

        string refreshToken = GenerateRefreshToken();

        return new AccessToken
        {
            Token = new JwtSecurityTokenHandler().WriteToken(jwt),
            ExpiresAtUtc = accessExpiration,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = now.AddDays(_options.RefreshTokenExpirationDays)
        };
    }

    public string HashRefreshToken(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hash);
    }

    private static List<Claim> BuildClaims(User user, IEnumerable<string> operationClaims)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(AppClaimTypes.Nickname, user.Nickname),
            new(AppClaimTypes.SecurityStamp, user.SecurityStamp),

            // jti: her jetona tekil kimlik. Denetim kaydı ve gerekirse
            // tek bir jetonu kara listeye almak için gerekir.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(operationClaims.Select(claim => new Claim(ClaimTypes.Role, claim)));

        return claims;
    }

    /// <summary>
    /// 256 bit kriptografik rastgelelik. <c>Guid.NewGuid()</c> kullanılmaz:
    /// GUID v4 yalnızca 122 bit entropi taşır ve bazı üreticilerde tahmin
    /// edilebilirdir — jeton üretimi için uygun değildir.
    /// </summary>
    private static string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(RefreshTokenSizeBytes));
}
