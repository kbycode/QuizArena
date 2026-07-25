using Autofac;
using Autofac.Extras.DynamicProxy;
using QuizArena.BLL.Abstract;
using QuizArena.BLL.Concrete;
using QuizArena.Core.Aspects;
using Module = Autofac.Module;

namespace QuizArena.BLL.DependencyResolvers.Autofac;

/// <summary>
/// İş servislerini Autofac'e kaydeder ve <b>aspect boru hattını</b> devreye alır.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden yerleşik DI konteyneri değil de Autofac?</b>
/// .NET'in kendi konteyneri hızlı ve yeterlidir ama <b>metot kesme
/// (interception)</b> desteklemez. <c>[ValidationAspect]</c>,
/// <c>[CacheAspect]</c>, <c>[TransactionAspect]</c> gibi niteliklerin
/// çalışması, servis arayüzü için bir vekil (proxy) nesne üretilmesini
/// gerektirir; bunu Autofac + Castle DynamicProxy sağlıyor.
/// Uygulamanın geri kalanı (repository'ler, altyapı) yerleşik konteynerde
/// kayıtlı kalıyor — Autofac yalnızca ihtiyaç duyulan yerde devrede.
/// </para>
/// <para>
/// <c>EnableInterfaceInterceptors()</c> zorunlu: interception arayüz üzerinden
/// yapılır. Bu yüzden servisler her zaman arayüzleriyle (<c>IAuthService</c>)
/// enjekte edilmelidir; somut tip (<c>AuthManager</c>) enjekte edilirse vekil
/// devreden çıkar ve <b>aspect'ler sessizce çalışmaz</b>. Bu, AOP kullanan
/// projelerde en sık yapılan hatadır.
/// </para>
/// </remarks>
public sealed class AutofacBusinessModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        RegisterService<AuthManager, IAuthService>(builder);
        RegisterService<UserManager, IUserService>(builder);
        RegisterService<CategoryManager, ICategoryService>(builder);
        RegisterService<QuestionManager, IQuestionService>(builder);
        RegisterService<RoomManager, IRoomService>(builder);
        RegisterService<GameManager, IGameService>(builder);
        RegisterService<StatisticManager, IStatisticService>(builder);
        RegisterService<AchievementManager, IAchievementService>(builder);
        RegisterService<LeaderboardManager, ILeaderboardService>(builder);
        RegisterService<OperationClaimManager, IOperationClaimService>(builder);
    }

    /// <summary>
    /// Bir iş servisini, aspect kesicisi bağlı hâlde kaydeder.
    /// </summary>
    /// <remarks>
    /// <c>InstancePerLifetimeScope</c> = ASP.NET Core'daki <c>Scoped</c>:
    /// servis, istek boyunca aynı <c>DbContext</c> ve aynı
    /// <c>IUnitOfWork</c> ile çalışır. <c>Singleton</c> olsaydı istek başına
    /// yaratılan <c>DbContext</c>'i tutup "disposed context" hatalarına
    /// yol açardı.
    /// </remarks>
    private static void RegisterService<TImplementation, TService>(ContainerBuilder builder)
        where TImplementation : notnull, TService
        where TService : notnull
    {
        builder.RegisterType<TImplementation>()
            .As<TService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectInterceptor))
            .InstancePerLifetimeScope();
    }
}
