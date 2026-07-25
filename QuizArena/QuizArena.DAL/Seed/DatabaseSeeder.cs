using System.Security.Cryptography;
using QuizArena.Core.Entities.Concrete;
using QuizArena.Core.Utilities.Clock;
using QuizArena.Core.Utilities.Security.Hashing;
using QuizArena.DAL.Contexts;
using QuizArena.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QuizArena.DAL.Seed;

/// <summary>
/// Veritabanını uygulamanın çalışması için gereken referans veriyle doldurur.
/// </summary>
/// <remarks>
/// <para>
/// <b>Fikirsel olarak "idempotent"tir:</b> kaç kez çağrılırsa çağrılsın aynı
/// sonucu üretir, veriyi çoğaltmaz. Bu yüzden her açılışta güvenle
/// çalıştırılabilir.
/// </para>
/// <para>
/// EF Core'un <c>HasData</c> mekanizması yerine çalışma zamanı seed'i tercih
/// edildi; çünkü parola özeti üretmek gibi <b>çalışma zamanına ait</b> işler
/// migration dosyasına gömülemez (gömülse, özet migration'ın içinde sabit
/// kalır ve depoya yazılır).
/// </para>
/// </remarks>
public sealed class DatabaseSeeder
{
    private readonly QuizArenaDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;
    private readonly SeedOptions _options;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        QuizArenaDbContext context,
        IPasswordHasher passwordHasher,
        IClock clock,
        IOptions<SeedOptions> options,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedOperationClaimsAsync(cancellationToken);
        await SeedAchievementsAsync(cancellationToken);
        await SeedCategoriesAndQuestionsAsync(cancellationToken);
        await SeedAdminUserAsync(cancellationToken);

        if (_options.CreateDemoPlayers)
        {
            await SeedDemoPlayersAsync(cancellationToken);
        }
    }

    // ---------------------------------------------------------------------
    //  Yetkiler
    // ---------------------------------------------------------------------
    private async Task SeedOperationClaimsAsync(CancellationToken cancellationToken)
    {
        List<string> existing = await _context.OperationClaims
            .Select(oc => oc.Name)
            .ToListAsync(cancellationToken);

        var missing = SeedRoles.All
            .Where(pair => !existing.Contains(pair.Key))
            .Select(pair => new OperationClaim { Name = pair.Key, Description = pair.Value })
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        _context.OperationClaims.AddRange(missing);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("{Count} yetki tanımı eklendi.", missing.Count);
    }

    // ---------------------------------------------------------------------
    //  Rozetler
    // ---------------------------------------------------------------------
    private async Task SeedAchievementsAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await _context.Achievements
            .Select(a => a.Code)
            .ToListAsync(cancellationToken);

        var missing = SeedAchievements.Create()
            .Where(a => !existingCodes.Contains(a.Code))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        _context.Achievements.AddRange(missing);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("{Count} rozet tanımı eklendi.", missing.Count);
    }

    // ---------------------------------------------------------------------
    //  Kategoriler ve sorular
    // ---------------------------------------------------------------------
    private async Task SeedCategoriesAndQuestionsAsync(CancellationToken cancellationToken)
    {
        List<string> existingSlugs = await _context.Categories
            .Select(c => c.Slug)
            .ToListAsync(cancellationToken);

        var newCategories = new List<Category>();

        foreach (SeedCategory seed in SeedContent.Categories)
        {
            if (existingSlugs.Contains(seed.Slug))
            {
                continue;
            }

            var category = new Category
            {
                Name = seed.Name,
                Slug = seed.Slug,
                Description = seed.Description,
                Icon = seed.Icon,
                ColorHex = seed.ColorHex,
                DisplayOrder = seed.DisplayOrder,
                IsActive = true
            };

            foreach (SeedQuestion seedQuestion in seed.Questions)
            {
                var question = new Question
                {
                    CategoryId = category.Id,
                    Text = seedQuestion.Text,
                    Difficulty = seedQuestion.Difficulty,
                    Explanation = seedQuestion.Explanation,
                    // Zor sorulara daha çok süre: zorluk hem puanı hem süreyi etkiler.
                    TimeLimitSeconds = seedQuestion.Difficulty switch
                    {
                        Entities.Enums.QuestionDifficulty.Easy => 15,
                        Entities.Enums.QuestionDifficulty.Medium => 20,
                        _ => 25
                    },
                    IsActive = true
                };

                for (int i = 0; i < seedQuestion.Options.Length; i++)
                {
                    question.Answers.Add(new Answer
                    {
                        QuestionId = question.Id,
                        Text = seedQuestion.Options[i],
                        IsCorrect = i == seedQuestion.CorrectIndex,
                        DisplayOrder = i + 1
                    });
                }

                category.Questions.Add(question);
            }

            newCategories.Add(category);
        }

        if (newCategories.Count == 0)
        {
            return;
        }

        _context.Categories.AddRange(newCategories);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "{CategoryCount} kategori ve {QuestionCount} soru eklendi.",
            newCategories.Count,
            newCategories.Sum(c => c.Questions.Count));
    }

    // ---------------------------------------------------------------------
    //  Yönetici hesabı
    // ---------------------------------------------------------------------
    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        string normalizedEmail = _options.AdminEmail.Trim().ToUpperInvariant();

        if (await _context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            return;
        }

        string? password = _options.AdminPassword;

        if (string.IsNullOrWhiteSpace(password))
        {
            if (!_options.AllowGeneratedAdminPassword)
            {
                // Üretimde tahmin edilebilir bir yönetici hesabı oluşturmak,
                // uygulamayı açık kapıyla yayına almak demektir.
                _logger.LogWarning(
                    "Seed:AdminPassword tanımlı olmadığı için yönetici hesabı OLUŞTURULMADI. " +
                    "Ortam değişkeni 'Seed__AdminPassword' ile bir parola verip uygulamayı yeniden başlatın.");
                return;
            }

            password = GenerateStrongPassword();

            // Yalnızca geliştirme ortamında ve yalnızca bir kez: hesap
            // oluşturulduktan sonra parola bir daha hiçbir yerde görünmez.
            _logger.LogWarning(
                "GELİŞTİRME ORTAMI: yönetici parolası üretildi → {Email} / {Password}\n" +
                "Bu satır yalnızca bir kez yazılır. Kalıcı bir parola için: " +
                "dotnet user-secrets set \"Seed:AdminPassword\" \"<parola>\"",
                _options.AdminEmail,
                password);
        }

        var admin = new User
        {
            Email = _options.AdminEmail.Trim(),
            NormalizedEmail = normalizedEmail,
            FirstName = "Sistem",
            LastName = "Yöneticisi",
            Nickname = _options.AdminNickname,
            PasswordHash = _passwordHasher.Hash(password),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            IsActive = true
        };

        _context.Users.Add(admin);
        _context.UserStatistics.Add(new UserStatistic { UserId = admin.Id });

        // Yönetici tüm yetkileri alır.
        List<OperationClaim> claims = await _context.OperationClaims.ToListAsync(cancellationToken);
        foreach (OperationClaim claim in claims)
        {
            _context.UserOperationClaims.Add(new UserOperationClaim
            {
                UserId = admin.Id,
                OperationClaimId = claim.Id
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Yönetici hesabı oluşturuldu: {Email}", admin.Email);
    }

    // ---------------------------------------------------------------------
    //  Demo oyuncular (yalnızca geliştirme/demo)
    // ---------------------------------------------------------------------
    /// <remarks>
    /// Sıralama tablosunun boş görünmemesi için örnek veri üretir.
    /// Hesaplar rastgele ve <b>hiçbir yere yazılmayan</b> parolalarla
    /// oluşturulur; yani bu hesaplarla oturum açılamaz — yalnızca listede
    /// görünürler. E-posta alan adı olarak, standartlarca (RFC 2606) hiçbir
    /// zaman gerçek olamayacak <c>.test</c> uzantısı kullanılır.
    /// </remarks>
    private async Task SeedDemoPlayersAsync(CancellationToken cancellationToken)
    {
        (string Nickname, int Score, int Competitions, int Answered, int Correct, int Streak)[] demoPlayers =
        [
            ("BilgiAvcisi", 12_450, 34, 340, 291, 18),
            ("SoruCanbazi", 9_820, 27, 270, 214, 14),
            ("KronikOkur", 7_310, 21, 210, 166, 11),
            ("HizliParmak", 5_640, 16, 160, 118, 9)
        ];

        var created = 0;

        foreach (var demo in demoPlayers)
        {
            string email = $"{demo.Nickname.ToLowerInvariant()}@ornek.test";
            string normalized = email.ToUpperInvariant();

            if (await _context.Users.AnyAsync(u => u.NormalizedEmail == normalized, cancellationToken))
            {
                continue;
            }

            var user = new User
            {
                Email = email,
                NormalizedEmail = normalized,
                FirstName = "Örnek",
                LastName = "Oyuncu",
                Nickname = demo.Nickname,
                // Parola üretilir ama saklanmaz/loglanmaz: giriş yapılamaz.
                PasswordHash = _passwordHasher.Hash(GenerateStrongPassword()),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                IsActive = true
            };

            _context.Users.Add(user);
            _context.UserStatistics.Add(new UserStatistic
            {
                UserId = user.Id,
                TotalScore = demo.Score,
                TotalCompetitions = demo.Competitions,
                TotalQuestionsAnswered = demo.Answered,
                TotalCorrectAnswers = demo.Correct,
                BestStreak = demo.Streak,
                BestCompetitionScore = demo.Score / Math.Max(demo.Competitions, 1) * 2,
                WinCount = demo.Competitions / 3,
                AverageAnswerMilliseconds = 4_200,
                LastPlayedAtUtc = _clock.UtcNow.AddDays(-created - 1)
            });

            created++;
        }

        if (created == 0)
        {
            return;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("{Count} demo oyuncu ve istatistiği eklendi.", created);
    }

    /// <summary>
    /// Kriptografik olarak güçlü, okunabilir parola üretir.
    /// </summary>
    private static string GenerateStrongPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";   // I, O çıkarıldı
        const string lower = "abcdefghijkmnopqrstuvwxyz";   // l çıkarıldı
        const string digits = "23456789";                   // 0, 1 çıkarıldı
        const string symbols = "!@#$%*?-_";

        string alphabet = upper + lower + digits + symbols;

        // Her karakter kümesinden en az bir tane olsun ki üretilen parola
        // uygulamanın kendi parola politikasını da geçsin.
        char[] password =
        [
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            symbols[RandomNumberGenerator.GetInt32(symbols.Length)],
            .. Enumerable.Range(0, 12).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)])
        ];

        // Fisher-Yates: zorunlu karakterlerin hep başta olmasını engeller.
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}
