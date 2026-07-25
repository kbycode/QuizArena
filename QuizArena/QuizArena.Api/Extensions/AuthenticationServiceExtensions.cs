using System.Text;
using QuizArena.Core.Utilities.Security.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace QuizArena.Api.Extensions;

/// <summary>JWT kimlik doğrulama yapılandırması.</summary>
public static class AuthenticationServiceExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Ayarlar, DI'a kayıtlı ve açılışta doğrulanmış TokenOptions'tan
                // okunur. Orijinal kodda 'Configuration.Get<TokenOptions>()'
                // sonucu null kontrolü olmadan kullanılıyordu: ayar eksikse
                // NullReferenceException ile açılış patlıyordu.
                options.TokenValidationParameters = BuildValidationParameters(services);

                // Süresi geçmiş jeton için istemciye net bir sinyal ver:
                // arayüz bu başlığı görüp sessizce jeton yenilemeye gidebilir.
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Append("X-Token-Expired", "true");
                        }

                        return Task.CompletedTask;
                    },

                    // SignalR tarayıcıda WebSocket başlığı ekleyemediği için
                    // jetonu sorgu dizesinde taşır; hub istekleri için oradan okuyoruz.
                    OnMessageReceived = context =>
                    {
                        string? accessToken = context.Request.Query["access_token"];
                        PathString path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    private static TokenValidationParameters BuildValidationParameters(IServiceCollection services)
    {
        // Ayarları geçici bir sağlayıcıdan okuyoruz: JwtBearer yapılandırması
        // DI kapsabı kurulmadan önce çalıştığı için normal enjeksiyon mümkün değil.
        using ServiceProvider provider = services.BuildServiceProvider();
        TokenOptions tokenOptions = provider.GetRequiredService<IOptions<TokenOptions>>().Value;

        return new TokenValidationParameters
        {
            // Aşağıdaki beş kontrolün hepsi açık olmak zorunda.
            // Örneğin ValidateIssuerSigningKey kapatılırsa, imzası bize ait
            // olmayan bir jeton kabul edilir; yani herkes kendi jetonunu
            // üretip Admin rolüyle gelebilir.
            ValidateIssuer = true,
            ValidIssuer = tokenOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = tokenOptions.Audience,

            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenOptions.SecurityKey)),

            // Varsayılan 5 dakikalık saat toleransı, 15 dakikalık bir jetonun
            // fiilen 20 dakika yaşaması demektir. Kısa ömürlü jetonun anlamını
            // korumak için toleransı düşürüyoruz.
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }
}
