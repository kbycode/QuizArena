using QuizArena.DAL.Contexts;
using QuizArena.DAL.Seed;
using Microsoft.EntityFrameworkCore;

namespace QuizArena.Api.Extensions;

/// <summary>Açılışta veritabanı şemasını ve başlangıç verisini hazırlar.</summary>
public static class DatabaseInitializationExtensions
{
    /// <remarks>
    /// <para>
    /// <b>Migration yalnızca geliştirme ortamında otomatik uygulanır.</b>
    /// Sebebi: üretimde uygulama birden fazla örnek (instance) hâlinde
    /// çalışır ve hepsi aynı anda açılırken aynı migration'ı uygulamaya
    /// çalışır. Bu, kilitlenmeye veya yarım uygulanmış şemaya yol açar.
    /// Üretimde şema, dağıtım hattında (CI/CD) tek bir adımda uygulanır.
    /// </para>
    /// <para>
    /// Seed ise her ortamda güvenle çalışır: kaç kez çağrılırsa çağrılsın
    /// aynı sonucu üretir (idempotent) ve eksik referans veriyi tamamlar.
    /// </para>
    /// </remarks>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();

        ILogger<WebApplication> logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<WebApplication>();

        var context = scope.ServiceProvider.GetRequiredService<QuizArenaDbContext>();

        try
        {
            if (app.Environment.IsDevelopment())
            {
                logger.LogInformation("Veritabanı şeması güncelleniyor (migration)…");
                await context.Database.MigrateAsync();
            }
            else if (!await context.Database.CanConnectAsync())
            {
                // Üretimde şema uygulamıyoruz; ama bağlanamıyorsak bunu
                // sessizce geçmek yerine net bir hatayla duruyoruz.
                throw new InvalidOperationException(
                    "Veritabanına bağlanılamadı. Bağlantı dizesini ve şemanın uygulandığını doğrulayın.");
            }

            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();

            logger.LogInformation("Veritabanı hazır.");
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "Veritabanı hazırlanamadı. Uygulama başlatılamıyor.");
            throw;
        }
    }
}
