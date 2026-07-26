using Microsoft.EntityFrameworkCore;
using QuizArena.DAL.Abstract;
using QuizArena.DAL.Contexts;
using QuizArena.DAL.ReadModels;
using QuizArena.Entities.Enums;

namespace QuizArena.DAL.Concrete.EntityFramework;

/// <summary>
/// <see cref="IDashboardRepository"/>'nin Entity Framework Core uygulaması.
/// </summary>
/// <remarks>
/// <para>
/// <b>Sağlayıcıdan bağımsızlık bilinçli.</b> Burada <c>EF.Functions.DateDiff*</c>
/// gibi SQL Server'a özgü hiçbir çağrı yok; testler SQLite üzerinde koştuğu
/// için sağlayıcıya özgü SQL, panoyu test edilemez hâle getirirdi. Tarih
/// gruplaması <c>DateTime.Date</c> ile yapılıyor; her iki sağlayıcı da bunu
/// çeviriyor.
/// </para>
/// <para>
/// <b>Yumuşak silme:</b> tüm sorgular <c>DbSet</c> üzerinden gittiği için
/// genel sorgu filtreleri (global query filters) kendiliğinden uygulanır;
/// silinmiş kayıtlar sayaçlara karışmaz.
/// </para>
/// </remarks>
public sealed class EfDashboardRepository : IDashboardRepository
{
    private readonly QuizArenaDbContext _context;

    public EfDashboardRepository(QuizArenaDbContext context) => _context = context;

    public async Task<DashboardCounters> GetCountersAsync(
        DateTime windowStartUtc,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        // Kullanıcı sayaçları tek geçişte: aynı tabloyu dört kez taramak
        // yerine tek gruplamayla toplanıyor.
        var userCounters = await _context.Users
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(u => u.IsActive),
                New = g.Count(u => u.CreatedAtUtc >= windowStartUtc),
                Locked = g.Count(u => u.LockoutEndUtc != null && u.LockoutEndUtc > nowUtc),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var categoryCounters = await _context.Categories
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Active = g.Count(c => c.IsActive) })
            .FirstOrDefaultAsync(cancellationToken);

        // Soru sayaçları + cevap toplamları aynı tablodan geldiği için birlikte.
        var questionCounters = await _context.Questions
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(q => q.IsActive),
                // long: sorulma sayıları zamanla int sınırını zorlayabilir ve
                // SUM taşması sessiz bir hataya dönüşür.
                Asked = g.Sum(q => (long)q.TimesAsked),
                Correct = g.Sum(q => (long)q.TimesAnsweredCorrectly),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var competitionCounters = await _context.Competitions
            .AsNoTracking()
            .Where(c => c.Status == CompetitionStatus.Completed)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                InWindow = g.Count(c => c.FinishedAtUtc != null && c.FinishedAtUtc >= windowStartUtc),
            })
            .FirstOrDefaultAsync(cancellationToken);

        int liveRooms = await _context.Rooms
            .AsNoTracking()
            .CountAsync(
                r => r.Status == RoomStatus.Waiting || r.Status == RoomStatus.InProgress,
                cancellationToken);

        return new DashboardCounters(
            TotalUsers: userCounters?.Total ?? 0,
            ActiveUsers: userCounters?.Active ?? 0,
            NewUsersInWindow: userCounters?.New ?? 0,
            LockedUsers: userCounters?.Locked ?? 0,
            TotalCategories: categoryCounters?.Total ?? 0,
            ActiveCategories: categoryCounters?.Active ?? 0,
            TotalQuestions: questionCounters?.Total ?? 0,
            ActiveQuestions: questionCounters?.Active ?? 0,
            FinishedCompetitions: competitionCounters?.Total ?? 0,
            CompetitionsInWindow: competitionCounters?.InWindow ?? 0,
            LiveRooms: liveRooms,
            TotalAnswers: questionCounters?.Asked ?? 0,
            TotalCorrectAnswers: questionCounters?.Correct ?? 0);
    }

    /// <remarks>
    /// <b>Neden iki sorgu?</b> "Gün başına yarışma" ile "gün başına benzersiz
    /// oyuncu" tek gruplamada istenirse, ikincisi <c>GroupBy</c> projeksiyonu
    /// içinde <c>Distinct().Count()</c> gerektirir; EF Core bunu SQL'e
    /// <b>çeviremez</b> ve sorgu çalışma anında patlar (derleme hatası vermez —
    /// bu yüzden yalnızca uygulamayı gerçekten çalıştırınca görülür).
    /// <para>
    /// Çözüm, benzersizliği gruplamadan <b>önce</b> uygulamak: önce
    /// (gün, kullanıcı) çiftleri tekilleştirilir, sonra güne göre sayılır.
    /// Bu biçim her iki sağlayıcıda da tek <c>SELECT DISTINCT … GROUP BY</c>
    /// cümlesine çevrilir.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<DailyActivityRow>> GetDailyActivityAsync(
        DateTime fromUtc,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Entities.Concrete.Competition> finished = _context.Competitions
            .AsNoTracking()
            .Where(c => c.Status == CompetitionStatus.Completed
                        && c.FinishedAtUtc != null
                        && c.FinishedAtUtc >= fromUtc);

        List<DayCount> competitionsPerDay = await finished
            .GroupBy(c => c.FinishedAtUtc!.Value.Date)
            .Select(g => new DayCount(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        List<DayCount> playersPerDay = await finished
            .Select(c => new { Day = c.FinishedAtUtc!.Value.Date, c.RoomParticipant.UserId })
            .Distinct()
            .GroupBy(pair => pair.Day)
            .Select(g => new DayCount(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        Dictionary<DateTime, int> playerLookup = playersPerDay.ToDictionary(x => x.Day, x => x.Count);

        return competitionsPerDay
            .OrderBy(x => x.Day)
            .Select(x => new DailyActivityRow(
                x.Day,
                x.Count,
                playerLookup.TryGetValue(x.Day, out int players) ? players : 0))
            .ToArray();
    }

    public async Task<IReadOnlyList<CategoryBreakdownRow>> GetCategoryBreakdownAsync(
        CancellationToken cancellationToken = default)
        => await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryBreakdownRow(
                c.Id,
                c.Name,
                c.Icon,
                c.ColorHex,
                c.IsActive,
                c.Questions.Count(q => q.IsActive),
                // Oda üzerinden sayıyoruz: kategori ile yarışma arasındaki tek
                // bağ odadır (yarışma kategoriyi doğrudan taşımaz).
                c.Rooms.Count(r => r.Status == RoomStatus.Finished),
                c.Questions.Sum(q => (long)q.TimesAsked),
                c.Questions.Sum(q => (long)q.TimesAnsweredCorrectly)))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<QuestionStatisticRow>> GetQuestionsBySuccessRateAsync(
        int top,
        bool ascending,
        int minimumTimesAsked,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Entities.Concrete.Question> query = _context.Questions
            .AsNoTracking()
            .Where(q => q.TimesAsked >= minimumTimesAsked);

        // Oran hesabı sıralama içinde yapılıyor; veritabanı bunu tek geçişte
        // çözer. Filtre `TimesAsked >= minimumTimesAsked` olduğu ve eşiğin
        // en az 1 olması sağlandığı için sıfıra bölme oluşamaz.
        IOrderedQueryable<Entities.Concrete.Question> ordered = ascending
            ? query.OrderBy(q => q.TimesAnsweredCorrectly * 1.0 / q.TimesAsked)
                   .ThenByDescending(q => q.TimesAsked)
            : query.OrderByDescending(q => q.TimesAnsweredCorrectly * 1.0 / q.TimesAsked)
                   .ThenByDescending(q => q.TimesAsked);

        return await ordered
            .Take(top)
            .Select(q => new QuestionStatisticRow(
                q.Id,
                q.Text,
                q.Category.Name,
                q.Difficulty,
                q.TimesAsked,
                q.TimesAnsweredCorrectly))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Gün → sayı ara sonucu (yalnızca bu sınıfa ait).</summary>
    private sealed record DayCount(DateTime Day, int Count);
}
