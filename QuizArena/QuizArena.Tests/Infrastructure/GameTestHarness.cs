using QuizArena.BLL.Abstract;
using QuizArena.BLL.Concrete;
using QuizArena.BLL.Notifications;
using QuizArena.Core.Entities.Concrete;
using QuizArena.DAL.Concrete.EntityFramework;
using QuizArena.DAL.Contexts;
using QuizArena.DAL.Seed;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace QuizArena.Tests.Infrastructure;

/// <summary>
/// Oyun akışı testleri için nesne grafiğini kuran yardımcı.
/// </summary>
/// <remarks>
/// <para>
/// Servisler burada <b>elle</b> kuruluyor, DI konteyneri kullanılmıyor. Sebep:
/// testin neye bağlı olduğunu görünür kılmak. Konteyner kullanıldığında bir
/// testin hangi bileşenleri gerçekten çalıştırdığı gizlenir.
/// </para>
/// <para>
/// <b>Önemli:</b> Aspect'ler (validation/cache/transaction) bu grafikte
/// devrede değil; onlar Autofac vekilleri üzerinden çalışıyor. Buradaki
/// testler bilinçli olarak <b>iş kurallarını</b> ölçüyor — aspect davranışı
/// ayrı ilgi alanı. Bu yüzden transaction sınırının yokluğu testleri
/// etkilemez: repository'ler kendi kayıtlarını zaten yazıyor.
/// </para>
/// </remarks>
public sealed class GameTestHarness : IAsyncDisposable
{
    private readonly SqliteTestDatabase _database;
    private readonly QuizArenaDbContext _context;

    private GameTestHarness(SqliteTestDatabase database, FakeClock clock)
    {
        _database = database;
        Clock = clock;
        _context = database.CreateContext();

        CurrentUser = new FakeCurrentUserService();

        // --- Repository'ler -------------------------------------------------
        var users = new EfUserRepository(_context);
        var categories = new EfCategoryRepository(_context);
        var questions = new EfQuestionRepository(_context);
        var rooms = new EfRoomRepository(_context);
        var participants = new EfRoomParticipantRepository(_context);
        var competitions = new EfCompetitionRepository(_context);
        var competitionQuestions = new EfCompetitionQuestionRepository(_context);
        var competitionAnswers = new EfCompetitionAnswerRepository(_context);
        var statistics = new EfUserStatisticRepository(_context);
        var achievements = new EfAchievementRepository(_context);
        var userAchievements = new EfUserAchievementRepository(_context);

        // --- Servisler -------------------------------------------------------
        var achievementService = new AchievementManager(
            achievements, userAchievements, clock, NullLogger<AchievementManager>.Instance);

        var statisticService = new StatisticManager(
            statistics, users, achievementService, CurrentUser, clock);

        Rooms = new RoomManager(
            rooms, participants, categories, questions, competitions, competitionQuestions,
            CurrentUser, new NullGameNotifier(), clock, NullLogger<RoomManager>.Instance);

        Game = new GameManager(
            competitions, competitionQuestions, competitionAnswers, rooms, participants, questions,
            statisticService, achievementService, CurrentUser, new NullGameNotifier(), clock,
            NullLogger<GameManager>.Instance);

        Statistics = statisticService;
        Achievements = achievementService;
    }

    public FakeClock Clock { get; }
    public FakeCurrentUserService CurrentUser { get; }
    public IRoomService Rooms { get; }
    public IGameService Game { get; }
    public IStatisticService Statistics { get; }
    public IAchievementService Achievements { get; }

    /// <summary>Kategori, sorular, rozetler ve bir kullanıcı içeren hazır ortam kurar.</summary>
    public static async Task<GameTestHarness> CreateAsync(int questionCount = 5)
    {
        var clock = new FakeClock();
        var database = new SqliteTestDatabase(clock);
        var harness = new GameTestHarness(database, clock);

        await harness.SeedAsync(questionCount);

        return harness;
    }

    public Guid CategoryId { get; private set; }

    public async Task<User> CreateUserAsync(string nickname)
    {
        var user = new User
        {
            Email = $"{nickname.ToLowerInvariant()}@ornek.test",
            NormalizedEmail = $"{nickname.ToUpperInvariant()}@ORNEK.TEST",
            FirstName = "Test",
            LastName = "Oyuncu",
            Nickname = nickname,
            PasswordHash = "pbkdf2-sha256$1000$dGVzdA==$dGVzdA==",
            SecurityStamp = Guid.NewGuid().ToString("N"),
            IsActive = true
        };

        _context.Users.Add(user);
        _context.UserStatistics.Add(new UserStatistic { UserId = user.Id });
        await _context.SaveChangesAsync();

        return user;
    }

    /// <summary>Oturum açmış kullanıcıyı değiştirir.</summary>
    public void SignIn(User user) => CurrentUser.SetUser(user.Id, user.Email);

    /// <summary>Doğrudan veritabanına erişim (doğrulama amaçlı).</summary>
    public QuizArenaDbContext NewContext() => _database.CreateContext();

    private async Task SeedAsync(int questionCount)
    {
        _context.Achievements.AddRange(SeedAchievements.Create());

        var category = new Category
        {
            Name = "Test Kategorisi",
            Slug = "test-kategorisi",
            Icon = "🧪",
            ColorHex = "#6366F1",
            IsActive = true,
            DisplayOrder = 1
        };

        for (var i = 1; i <= questionCount; i++)
        {
            var question = new Question
            {
                CategoryId = category.Id,
                Text = $"{i}. test sorusu: hangisi doğru şıktır?",
                // Zorluk dönüşümlü atanıyor ki puanlama farklarını da görebilelim.
                Difficulty = (QuestionDifficulty)((i % 3) + 1),
                TimeLimitSeconds = 20,
                Explanation = $"{i}. sorunun açıklaması.",
                IsActive = true
            };

            for (var option = 1; option <= 4; option++)
            {
                question.Answers.Add(new Answer
                {
                    QuestionId = question.Id,
                    Text = $"Şık {option}",
                    // Her soruda ilk şık doğru: testin cevabı deterministik olsun.
                    IsCorrect = option == 1,
                    DisplayOrder = option
                });
            }

            category.Questions.Add(question);
        }

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        CategoryId = category.Id;
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _database.DisposeAsync();
    }
}
