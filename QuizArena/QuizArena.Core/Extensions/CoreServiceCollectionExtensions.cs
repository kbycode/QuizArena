using QuizArena.Core.Aspects;
using QuizArena.Core.CrossCuttingConcerns.Caching;
using QuizArena.Core.Interceptors;
using QuizArena.Core.Utilities.Clock;
using QuizArena.Core.Utilities.Security;
using QuizArena.Core.Utilities.Security.Hashing;
using QuizArena.Core.Utilities.Security.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace QuizArena.Core.Extensions;

/// <summary>
/// Core katmanının DI kayıtları. API projesi tek satırla
/// (<c>services.AddCoreServices(configuration)</c>) tüm altyapıyı alır;
/// katmanın iç yapısını bilmek zorunda kalmaz.
/// </summary>
public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        // --- Ayarlar: eksik/zayıf yapılandırmada uygulama AÇILIŞTA patlar -----
        services
            .AddOptions<TokenOptions>()
            .Bind(configuration.GetSection(TokenOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // --- Çapraz kesen servisler ------------------------------------------
        services.AddSingleton<IClock, SystemClock>();

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // Parola özetleme durumsuzdur → Singleton yeterli.
        services.AddSingleton<IPasswordHasher>(_ => new Pbkdf2PasswordHasher());

        // İmza anahtarı ayarlardan bir kez okunur → Singleton.
        services.AddSingleton<ITokenService, JwtTokenService>();

        // HttpContext'e bağlı → istek başına.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // --- AOP -------------------------------------------------------------
        // Aspect'lerin bağımlılıklarını istek kapsamından çözebilmesi için
        // kesici de Scoped olmalıdır.
        services.AddScoped<AspectInterceptor>();

        // --- EF Core kesicileri ----------------------------------------------
        services.AddScoped<AuditSaveChangesInterceptor>();

        return services;
    }
}
