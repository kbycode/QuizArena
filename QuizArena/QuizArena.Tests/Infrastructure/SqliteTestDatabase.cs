using QuizArena.Core.Interceptors;
using QuizArena.Core.Utilities.Clock;
using QuizArena.DAL.Contexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace QuizArena.Tests.Infrastructure;

/// <summary>
/// Test başına izole, bellek içi SQLite veritabanı.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden SQLite, neden EF Core InMemory değil?</b> InMemory sağlayıcısı
/// ilişkisel bir veritabanı değildir: tekil (unique) indeksleri, yabancı
/// anahtar kısıtlarını ve transaction'ları uygulamaz. Bu projedeki en kritik
/// güvenceler tam olarak bu kısıtlara dayanıyor (ör. aynı soruya ikinci cevabı
/// engelleyen tekil indeks). InMemory ile test etmek, onları test etmemek olurdu.
/// </para>
/// <para>
/// Bağlantı açık tutulur: SQLite'ta bellek içi veritabanı, son bağlantı
/// kapandığında yok olur. Bağlantıyı sınıf ömrü boyunca elde tutmak,
/// veritabanının test süresince yaşamasını sağlar.
/// </para>
/// </remarks>
public sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<QuizArenaDbContext> _options;

    public SqliteTestDatabase(IClock? clock = null)
    {
        Clock = clock ?? new FakeClock();

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<QuizArenaDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AuditSaveChangesInterceptor(Clock))
            // Test hatalarında "hangi sorgu?" sorusunu cevaplamak için.
            .EnableSensitiveDataLogging()
            .Options;

        using QuizArenaDbContext context = CreateContext();
        context.Database.EnsureCreated();
    }

    public IClock Clock { get; }

    /// <summary>
    /// Yeni bir bağlam örneği üretir.
    /// </summary>
    /// <remarks>
    /// Her test adımı için ayrı bağlam almak, "veri gerçekten kaydedildi mi?"
    /// sorusunu dürüstçe test etmeyi sağlar: aynı bağlamda okumak, değişiklik
    /// takipçisinin (change tracker) belleğinden okumak anlamına gelebilir.
    /// </remarks>
    public QuizArenaDbContext CreateContext() => new(_options);

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
