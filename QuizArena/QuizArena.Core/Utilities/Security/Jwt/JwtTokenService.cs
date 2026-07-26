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
/// Jeton üretiminde kolayca gözden kaçan üç ayrıntı burada bilinçle ele
/// alınıyor:
/// <list type="bullet">
///   <item>
///     <b>Zaman <c>UtcNow</c>, <c>Now</c> değil.</b> JWT'nin <c>exp</c> ve
///     <c>nbf</c> alanları tanım gereği UTC epoch'tur. Yerel saatle üretilen
///     bir jeton, UTC+3 bir sunucuda "3 saat sonra geçerli olacak"
///     (<c>nbf</c> gelecekte) diye reddedilir; UTC-5 bir sunucuda da
///     beklenenden 5 saat fazla yaşar.
///   </item>
///   <item>
///     <b>Ömür her çağrıda hesaplanır, kurucuda değil.</b> Bitiş tarihi nesne
///     kurulurken bir kez hesaplansaydı, servis uzun ömürlü kaydedildiğinde
///     üretilen tüm jetonlar aynı ve giderek geçmişe kayan bir bitiş tarihi
///     taşırdı.
///   </item>
///   <item>
///     <b>Ayarlar <c>IOptions</c> ile gelir.</b> <c>IConfiguration</c>'ı
///     doğrudan okumak, ayar eksik olduğunda hatayı çalışma anına ve
///     <c>NullReferenceException</c>'a bırakır; <c>IOptions</c> doğrulaması
///     açılışta yapar.
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
