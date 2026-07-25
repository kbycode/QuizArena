using System.Globalization;
using System.Threading.RateLimiting;
using QuizArena.Api.Configuration;
using QuizArena.DAL.Contexts;
using Microsoft.AspNetCore.RateLimiting;

namespace QuizArena.Api.Extensions;

/// <summary>CORS, hız sınırlama ve sağlık kontrolü kayıtları.</summary>
public static class SecurityServiceExtensions
{
    /// <summary>Kimlik doğrulama uçlarına uygulanan sıkı sınırın adı.</summary>
    public const string AuthRateLimitPolicy = "auth";

    public static IServiceCollection AddConfiguredCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
                          ?? new CorsOptions();

        services.AddCors(options =>
        {
            options.AddPolicy(CorsOptions.PolicyName, policy =>
            {
                if (corsOptions.AllowedOrigins.Length == 0)
                {
                    // Hiç kaynak tanımlanmamışsa CORS'u AÇMIYORUZ.
                    // "AllowAnyOrigin" varsayılanı, jetonla korunan bir API'yi
                    // herhangi bir sitenin tarayıcıdan kullanmasına açar.
                    return;
                }

                policy
                    .WithOrigins(corsOptions.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    // SignalR'ın tarayıcıdan çalışabilmesi için gerekli.
                    // AllowAnyOrigin ile birlikte kullanılamaz — bu da
                    // kaynakları açıkça listelemenin bir başka gerekçesi.
                    .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddConfiguredRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var limits = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
                     ?? new RateLimitingOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // İstemciye ne zaman tekrar deneyebileceğini söylüyoruz;
            // aksi hâlde istemci körlemesine yeniden dener ve sınırı büyütür.
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.ContentType = "application/problem+json; charset=utf-8";
                await context.HttpContext.Response.WriteAsync(
                    """{"title":"Çok fazla istek","status":429,"detail":"Kısa sürede çok fazla istek gönderdiniz. Lütfen biraz bekleyip tekrar deneyin."}""",
                    cancellationToken);
            };

            // --- Genel sınır: kimlik doğrulanmışsa kullanıcı, değilse IP başına ---
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                string partitionKey =
                    context.User.Identity?.IsAuthenticated == true
                        ? $"user:{context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value}"
                        : $"ip:{context.Connection.RemoteIpAddress}";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.GeneralPermitPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            // --- Kimlik doğrulama uçları için sıkı sınır ---
            options.AddPolicy(AuthRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    $"auth:{context.Connection.RemoteIpAddress}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.AuthPermitPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    public static IServiceCollection AddConfiguredHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            // Veritabanına gerçekten bağlanabiliyor muyuz? Uygulamanın ayakta
            // olması, bağımlılıklarının da ayakta olması anlamına gelmez;
            // yük dengeleyici bu farkı bilmek zorundadır.
            .AddDbContextCheck<QuizArenaDbContext>(
                name: "veritabani",
                tags: ["ready"]);

        return services;
    }
}
