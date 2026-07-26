using QuizArena.Core.DataAccess;
using QuizArena.Core.Interceptors;
using QuizArena.DAL.Abstract;
using QuizArena.DAL.Concrete.EntityFramework;
using QuizArena.DAL.Contexts;
using QuizArena.DAL.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace QuizArena.DAL.Extensions;

/// <summary>Veri erişim katmanının DI kayıtları.</summary>
public static class DataAccessServiceCollectionExtensions
{
    public const string ConnectionStringName = "QuizArena";

    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"'{ConnectionStringName}' bağlantı dizesi bulunamadı. " +
                "appsettings.json veya ortam değişkeni ile tanımlayın.");

        services.AddDbContext<QuizArenaDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(QuizArenaDbContext).Assembly.FullName);
                sql.CommandTimeout(30);

                // ---------------------------------------------------------------
                //  EnableRetryOnFailure BİLİNÇLİ OLARAK KAPALI
                //
                //  EF Core'un yeniden deneme stratejisi (SqlServerRetryingExecutionStrategy)
                //  "kullanıcı tarafından başlatılan transaction"ları desteklemez:
                //  BeginTransaction çağrıldığında
                //      "The configured execution strategy does not support
                //       user-initiated transactions"
                //  hatası atar. Sebebi mantıklı — strateji, hata alan işlemi
                //  baştan çalıştırır; ancak transaction'ın neresinden
                //  başlayacağını bilemez, dolayısıyla atomikliği garanti edemez.
                //
                //  Bu projede [TransactionAspect] transaction sınırını yönetiyor
                //  ve "yarışmayı bitir" gibi çok adımlı işlemlerin atomikliği
                //  yeniden denemeden daha kritik. Bu yüzden strateji kapalı.
                //
                //  Bulut veritabanına (ör. Azure SQL) taşınırken geçici hata
                //  dayanıklılığı geri kazanılmak istenirse iki yol var:
                //    1) Transaction gerektiren işlemleri
                //       Database.CreateExecutionStrategy().ExecuteAsync(...) ile
                //       tek bir yeniden denenebilir birim olarak sarmak,
                //    2) Yeniden denemeyi bir üst katmanda (HTTP istemcisi /
                //       Polly) yapmak.
                //  Sessizce ikisini birlikte açmak, üretimde ilk transaction'da
                //  patlayan bir uygulama demek olurdu.
                // ---------------------------------------------------------------
            });

            // Denetim alanlarını otomatik dolduran kesici.
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());

            // Not: Tembel yükleme (lazy loading) bilinçli olarak kullanılmıyor.
            // Proxies paketi hiç eklenmedi; ilişkiler her zaman açıkça
            // Include ile yüklenir. Tembel yükleme açık olsa, bir DTO
            // dönüştürme döngüsü içinde farkında olmadan her satır için ek
            // sorgu atılır (N+1) ve serileştirme sırasında tüm ilişki grafiği
            // çekilirdi.
        });

        // İş birimi: transaction sınırını yönetir.
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        // --- Repository kayıtları --------------------------------------------
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IOperationClaimRepository, EfOperationClaimRepository>();
        services.AddScoped<IUserOperationClaimRepository, EfUserOperationClaimRepository>();
        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();

        services.AddScoped<ICategoryRepository, EfCategoryRepository>();
        services.AddScoped<IQuestionRepository, EfQuestionRepository>();
        services.AddScoped<IAnswerRepository, EfAnswerRepository>();

        services.AddScoped<IRoomRepository, EfRoomRepository>();
        services.AddScoped<IRoomParticipantRepository, EfRoomParticipantRepository>();
        services.AddScoped<ICompetitionRepository, EfCompetitionRepository>();
        services.AddScoped<ICompetitionQuestionRepository, EfCompetitionQuestionRepository>();
        services.AddScoped<ICompetitionAnswerRepository, EfCompetitionAnswerRepository>();

        services.AddScoped<IUserStatisticRepository, EfUserStatisticRepository>();
        services.AddScoped<IAchievementRepository, EfAchievementRepository>();
        services.AddScoped<IUserAchievementRepository, EfUserAchievementRepository>();

        // Pano: varlık döndürmediği için EfEntityRepositoryBase'den türemez.
        services.AddScoped<IDashboardRepository, EfDashboardRepository>();

        // --- Başlangıç verisi ------------------------------------------------
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
