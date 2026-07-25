using System.Linq.Expressions;
using System.Reflection;
using QuizArena.Core.Entities;
using QuizArena.Core.Entities.Concrete;
using QuizArena.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace QuizArena.DAL.Contexts;

/// <summary>
/// Uygulamanın EF Core bağlamı.
/// </summary>
/// <remarks>
/// <para>
/// <b>İlk hâle göre en kritik iki değişiklik:</b>
/// </para>
/// <list type="number">
///   <item>
///     <b><c>OnConfiguring</c> kaldırıldı.</b> Bağlantı dizesi orada sabit
///     yazılıydı (<c>Server=.\SQLEXPRESS01;…</c>). Bunun anlamı: uygulama
///     yalnızca o tek makinede çalışır, test/üretim ortamı için ayrı derleme
///     gerekir ve bağlantı bilgisi kaynak koda (dolayısıyla depoya) gömülür.
///     Yapılandırma artık DI üzerinden <c>AddDbContext</c> ile verilir.
///   </item>
///   <item>
///     <b>Eşleme <c>OnModelCreating</c> içinde toplanmıyor.</b> Her varlığın
///     kuralları kendi <c>IEntityTypeConfiguration</c> sınıfında; bu sayede
///     bağlam dosyası varlık sayısıyla birlikte büyümüyor.
///   </item>
/// </list>
/// </remarks>
public class QuizArenaDbContext : DbContext
{
    public QuizArenaDbContext(DbContextOptions<QuizArenaDbContext> options)
        : base(options)
    {
    }

    // --- Kimlik / yetki (Core) ----------------------------------------------
    public DbSet<User> Users => Set<User>();
    public DbSet<OperationClaim> OperationClaims => Set<OperationClaim>();
    public DbSet<UserOperationClaim> UserOperationClaims => Set<UserOperationClaim>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // --- İçerik --------------------------------------------------------------
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Answer> Answers => Set<Answer>();

    // --- Oyun ----------------------------------------------------------------
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomParticipant> RoomParticipants => Set<RoomParticipant>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<CompetitionQuestion> CompetitionQuestions => Set<CompetitionQuestion>();
    public DbSet<CompetitionAnswer> CompetitionAnswers => Set<CompetitionAnswer>();

    // --- İstatistik / rozet --------------------------------------------------
    public DbSet<UserStatistic> UserStatistics => Set<UserStatistic>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Bu derlemedeki tüm IEntityTypeConfiguration<> sınıflarını uygular.
        // Yeni bir varlık eklendiğinde burayı düzenlemek gerekmez.
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    /// <summary>
    /// <see cref="ISoftDeletable"/> uygulayan her varlığa
    /// <c>WHERE IsDeleted = 0</c> filtresini <b>otomatik</b> ekler.
    /// </summary>
    /// <remarks>
    /// Filtreyi elle her sorguya yazmak, bir yerde unutulduğunda silinmiş
    /// kaydın kullanıcıya görünmesi anlamına gelir. Global filtre bunu
    /// yapısal olarak engeller; gerçekten silinmişleri de görmek gerektiğinde
    /// (yönetim/denetim ekranı) <c>IgnoreQueryFilters()</c> ile bilinçli olarak
    /// devre dışı bırakılır.
    /// <para>
    /// Filtre, yansımayla üretilen bir lambda ifadesiyle kurulur; her varlık
    /// tipi için elle bir satır yazma ihtiyacı ortadan kalkar.
    /// </para>
    /// </remarks>
    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            // e => !e.IsDeleted
            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "e");
            MemberExpression property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            LambdaExpression filter = Expression.Lambda(Expression.Not(property), parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}
