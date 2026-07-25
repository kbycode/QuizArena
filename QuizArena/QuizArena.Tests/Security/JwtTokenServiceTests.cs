using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using QuizArena.Core.Entities.Concrete;
using QuizArena.Core.Utilities.Security.Jwt;
using QuizArena.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace QuizArena.Tests.Security;

/// <summary>JWT üretim testleri.</summary>
public sealed class JwtTokenServiceTests
{
    private static readonly TokenOptions Options = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SecurityKey = "test-icin-uretilmis-en-az-32-karakterlik-anahtar",
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    };

    private static User CreateUser() => new()
    {
        Email = "oyuncu@ornek.test",
        NormalizedEmail = "OYUNCU@ORNEK.TEST",
        FirstName = "Ali",
        LastName = "Yılmaz",
        Nickname = "AliY",
        PasswordHash = "pbkdf2-sha256$1000$x$y",
        SecurityStamp = "damga-1"
    };

    private static (JwtTokenService Service, FakeClock Clock) CreateService()
    {
        var clock = new FakeClock();
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(Options), clock);
        return (service, clock);
    }

    [Fact]
    public void Jeton_beklenen_claimleri_tasir()
    {
        (JwtTokenService service, _) = CreateService();
        User user = CreateUser();

        AccessToken accessToken = service.CreateAccessToken(user, ["Admin", "Question.Manage"]);

        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);

        jwt.Issuer.Should().Be(Options.Issuer);
        jwt.Audiences.Should().Contain(Options.Audience);

        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == AppClaimTypes.SecurityStamp && c.Value == "damga-1");
        jwt.Claims.Should().Contain(c => c.Type == AppClaimTypes.Nickname && c.Value == "AliY");

        // Roller claim olarak yazılır; [Authorize(Roles = ...)] bunları okur.
        jwt.Claims.Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .Should().BeEquivalentTo("Admin", "Question.Manage");
    }

    [Fact]
    public void Jeton_omru_UTC_saate_gore_hesaplanir()
    {
        (JwtTokenService service, FakeClock clock) = CreateService();

        AccessToken accessToken = service.CreateAccessToken(CreateUser(), []);

        accessToken.ExpiresAtUtc.Should().Be(clock.UtcNow.AddMinutes(Options.AccessTokenExpirationMinutes));
        accessToken.RefreshTokenExpiresAtUtc.Should().Be(clock.UtcNow.AddDays(Options.RefreshTokenExpirationDays));
    }

    [Fact]
    public void Jeton_omru_her_uretimde_yeniden_hesaplanir()
    {
        // Orijinal JwtHelper, bitiş zamanını KURUCUDA bir kez hesaplıyordu.
        // Servis uzun ömürlü olduğunda tüm jetonlar aynı, giderek geçmişe
        // kayan bitiş tarihini taşıyordu. Bu test o hatanın geri gelmesini
        // engeller.
        (JwtTokenService service, FakeClock clock) = CreateService();

        AccessToken first = service.CreateAccessToken(CreateUser(), []);

        clock.Advance(TimeSpan.FromHours(2));

        AccessToken second = service.CreateAccessToken(CreateUser(), []);

        second.ExpiresAtUtc.Should().Be(first.ExpiresAtUtc.AddHours(2));
    }

    [Fact]
    public void Her_jeton_tekil_bir_yenileme_jetonu_uretir()
    {
        (JwtTokenService service, _) = CreateService();

        string[] refreshTokens = Enumerable.Range(0, 50)
            .Select(_ => service.CreateAccessToken(CreateUser(), []).RefreshToken)
            .ToArray();

        refreshTokens.Should().OnlyHaveUniqueItems();
        // 32 bayt → Base64'te 44 karakter.
        refreshTokens.Should().OnlyContain(token => token.Length == 44);
    }

    [Fact]
    public void Yenileme_jetonu_ozeti_deterministiktir()
    {
        (JwtTokenService service, _) = CreateService();
        const string token = "ornek-yenileme-jetonu";

        // Aynı girdi hep aynı özeti vermeli: veritabanında bu özetle arama
        // yapılıyor. Tuzlanmış bir özet aranamaz hâle gelirdi.
        service.HashRefreshToken(token).Should().Be(service.HashRefreshToken(token));
        service.HashRefreshToken(token).Should().NotBe(service.HashRefreshToken(token + "x"));
    }

    [Fact]
    public void Ham_yenileme_jetonu_ozetiyle_ayni_degildir()
    {
        (JwtTokenService service, _) = CreateService();

        AccessToken accessToken = service.CreateAccessToken(CreateUser(), []);

        // Veritabanına özet yazılır, ham jeton yalnızca istemciye gider.
        service.HashRefreshToken(accessToken.RefreshToken).Should().NotBe(accessToken.RefreshToken);
    }
}
