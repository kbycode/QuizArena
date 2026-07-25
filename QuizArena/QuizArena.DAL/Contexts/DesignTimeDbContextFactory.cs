using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QuizArena.DAL.Contexts;

/// <summary>
/// <c>dotnet ef</c> araçlarının tasarım zamanında bağlam üretebilmesi için fabrika.
/// </summary>
/// <remarks>
/// <para>
/// Bu sınıf olmasa <c>dotnet ef migrations add</c> komutu, bağlamı oluşturmak
/// için API projesinin <c>Program.cs</c>'ini çalıştırmak zorunda kalır; bu da
/// migration üretmek için geçerli bir <c>appsettings</c>, secret ve hatta
/// ayakta bir veritabanı gerektirmesi anlamına gelir.
/// </para>
/// <para>
/// Buradaki bağlantı dizesi <b>yalnızca şema üretiminde</b> kullanılır; hiçbir
/// zaman veriye bağlanmaz (EF yalnızca sağlayıcının SQL üreticisine ihtiyaç
/// duyar). Gerçek bağlantı bilgisi çalışma zamanında yapılandırmadan gelir.
/// Ortam değişkeni ile ezilebilir:
/// <c>ConnectionStrings__QuizArena=...</c>
/// </para>
/// </remarks>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<QuizArenaDbContext>
{
    private const string PlaceholderConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=QuizArenaDb;Trusted_Connection=True;TrustServerCertificate=True";

    public QuizArenaDbContext CreateDbContext(string[] args)
    {
        string connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__QuizArena")
            ?? PlaceholderConnectionString;

        DbContextOptions<QuizArenaDbContext> options =
            new DbContextOptionsBuilder<QuizArenaDbContext>()
                .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(QuizArenaDbContext).Assembly.FullName))
                .Options;

        return new QuizArenaDbContext(options);
    }
}
