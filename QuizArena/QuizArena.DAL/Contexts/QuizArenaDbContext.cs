using System.Linq.Expressions;
using System.Reflection;
using QuizArena.Core.Entities;
using QuizArena.Core.Entities.Concrete;
using QuizArena.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

        ApplyUtcDateTimeConverters(modelBuilder);
        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    /// <summary>
    /// Veritabanından okunan her <see cref="DateTime"/> değerini UTC olarak
    /// işaretler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Bu dönüştürücü olmadan sistemde sessiz bir saat kayması oluşur.</b>
    /// SQL Server'ın <c>datetime2</c> (ve SQLite'ın metin) sütunları saat
    /// dilimi bilgisi taşımaz; EF Core değeri geri okurken
    /// <see cref="DateTimeKind.Unspecified"/> atar. <c>System.Text.Json</c> ise
    /// yalnızca <c>Kind == Utc</c> olan değerlere <c>Z</c> son ekini yazar.
    /// Sonuç: API <c>"2026-07-26T07:22:00"</c> döner, tarayıcıdaki
    /// <c>new Date(...)</c> bunu <b>yerel saat</b> kabul eder ve UTC+3'teki
    /// kullanıcı, saat 10:22'de başlayacak etkinliği "3 saat önce başladı"
    /// diye görür.
    /// </para>
    /// <para>
    /// Hata özellikle sinsi: geliştirici makinesi UTC+0 ise <b>hiç
    /// görünmez</b>, testler de geçer — çünkü kayma sıfırdır. Yalnızca farklı
    /// bir saat diliminde gerçek uygulamayı çalıştırınca ortaya çıkar.
    /// </para>
    /// <para>
    /// Dönüşüm <b>tek yerde</b>, model kurulumunda uygulanıyor. Alternatifi
    /// her DTO eşlemesinde <c>DateTime.SpecifyKind</c> çağırmaktı; bir tanesi
    /// unutulduğunda aynı hata geri gelirdi. Yeni bir varlık veya tarih alanı
    /// eklendiğinde burada bir şey değiştirmek gerekmiyor.
    /// </para>
    /// </remarks>
    private static void ApplyUtcDateTimeConverters(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(UtcDateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(NullableUtcDateTimeConverter);
                }
            }
        }
    }

    /// <summary>
    /// Yazarken yerel saatleri UTC'ye çevirir, okurken değeri UTC olarak
    /// işaretler.
    /// </summary>
    /// <remarks>
    /// <c>Unspecified</c> değerler yazarken <b>olduğu gibi</b> bırakılıyor:
    /// bu projede tüm tarihler <c>IClock.UtcNow</c>'dan geliyor ve alan adları
    /// <c>...Utc</c> ile bitiyor, yani zaten UTC'ler. <c>ToUniversalTime()</c>
    /// çağırmak onları makinenin saat dilimi kadar kaydırırdı.
    /// </remarks>
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter = new(
        value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcDateTimeConverter = new(
        value => value.HasValue && value.Value.Kind == DateTimeKind.Local
            ? value.Value.ToUniversalTime()
            : value,
        value => value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value);

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
