using QuizArena.Core.DataAccess.EntityFramework;
using QuizArena.DAL.Abstract;
using QuizArena.DAL.Contexts;
using QuizArena.DAL.ReadModels;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace QuizArena.DAL.Concrete.EntityFramework;

public sealed class EfUserStatisticRepository
    : EfEntityRepositoryBase<UserStatistic, QuizArenaDbContext>, IUserStatisticRepository
{
    public EfUserStatisticRepository(QuizArenaDbContext context) : base(context) { }

    public Task<UserStatistic?> GetByUserAsync(
        Guid userId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
        => GetAsync(s => s.UserId == userId, asNoTracking: asNoTracking, cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<LeaderboardRow>> GetLeaderboardAsync(
        int top,
        CancellationToken cancellationToken = default)
        => await Context.UserStatistics
            .AsNoTracking()
            // Hiç oynamamış kullanıcılar sıralamayı kirletmesin.
            .Where(s => s.TotalCompetitions > 0 && s.User.IsActive)
            .OrderByDescending(s => s.TotalScore)
            .ThenByDescending(s => s.TotalCorrectAnswers)
            .Take(top)
            .Select(s => new LeaderboardRow(
                s.UserId,
                s.User.Nickname,
                s.User.AvatarUrl,
                s.TotalScore,
                s.TotalCompetitions,
                s.TotalQuestionsAnswered,
                s.TotalCorrectAnswers,
                s.BestStreak))
            .ToListAsync(cancellationToken);

    /// <remarks>
    /// Sıra numarası, "benden yüksek puanlı kaç kişi var + 1" olarak
    /// hesaplanır. Alternatifi tüm tabloyu sıralayıp satır numarası
    /// (<c>ROW_NUMBER()</c>) üretip içinde arama yapmaktı; bu sayım ise
    /// puan indeksinden doğrudan karşılanır.
    /// </remarks>
    public async Task<int> GetRankAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        int? score = await Context.UserStatistics
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => (int?)s.TotalScore)
            .FirstOrDefaultAsync(cancellationToken);

        if (score is null)
        {
            return 0;
        }

        int higher = await Context.UserStatistics
            .AsNoTracking()
            .CountAsync(s => s.TotalCompetitions > 0 && s.TotalScore > score.Value, cancellationToken);

        return higher + 1;
    }
}

public sealed class EfAchievementRepository
    : EfEntityRepositoryBase<Achievement, QuizArenaDbContext>, IAchievementRepository
{
    public EfAchievementRepository(QuizArenaDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Achievement>> GetByCodesAsync(
        IReadOnlyCollection<AchievementCode> codes,
        CancellationToken cancellationToken = default)
    {
        if (codes.Count == 0)
        {
            return [];
        }

        return await Context.Achievements
            .AsNoTracking()
            .Where(a => codes.Contains(a.Code))
            .ToListAsync(cancellationToken);
    }
}

public sealed class EfUserAchievementRepository
    : EfEntityRepositoryBase<UserAchievement, QuizArenaDbContext>, IUserAchievementRepository
{
    public EfUserAchievementRepository(QuizArenaDbContext context) : base(context) { }

    public async Task<IReadOnlyList<AchievementCode>> GetEarnedCodesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await Context.UserAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.Achievement.Code)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserAchievement>> GetByUserWithAchievementAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await Context.UserAchievements
            .AsNoTracking()
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == userId)
            .OrderByDescending(ua => ua.EarnedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserAchievement>> GetByCompetitionAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
        => await Context.UserAchievements
            .AsNoTracking()
            .Include(ua => ua.Achievement)
            .Where(ua => ua.CompetitionId == competitionId)
            .ToListAsync(cancellationToken);
}
