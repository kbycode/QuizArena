using QuizArena.Core.DataAccess.EntityFramework;
using QuizArena.Core.DataAccess.Paging;
using QuizArena.DAL.Abstract;
using QuizArena.DAL.Contexts;
using QuizArena.DAL.ReadModels;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace QuizArena.DAL.Concrete.EntityFramework;

public sealed class EfRoomRepository
    : EfEntityRepositoryBase<Room, QuizArenaDbContext>, IRoomRepository
{
    public EfRoomRepository(QuizArenaDbContext context) : base(context) { }

    public Task<Room?> GetByJoinCodeAsync(
        string joinCode,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
        => GetAsync(
            r => r.JoinCode == joinCode,
            include: q => q.Include(r => r.Participants),
            asNoTracking: asNoTracking,
            cancellationToken: cancellationToken);

    public Task<Room?> GetDetailAsync(
        Guid id,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
        => GetAsync(
            r => r.Id == id,
            include: q => q
                .Include(r => r.Category)
                .Include(r => r.HostUser)
                .Include(r => r.Participants)
                    .ThenInclude(p => p.User),
            asNoTracking: asNoTracking,
            cancellationToken: cancellationToken);

    public Task<PagedList<Room>> GetJoinableRoomsAsync(
        PageRequest pageRequest,
        CancellationToken cancellationToken = default)
        => GetPagedListAsync(
            pageRequest,
            predicate: r => r.Status == RoomStatus.Waiting
                            && r.Mode == RoomMode.PublicMultiplayer
                            // Etkinlikler kendi listelerinde gösterilir; sıradan
                            // "hemen katıl" listesine karışmamalılar.
                            && !r.IsOfficialEvent
                            // Kapasitesi dolmuş odayı listelemenin anlamı yok.
                            && r.Participants.Count < r.MaxPlayers,
            orderBy: q => q.OrderByDescending(r => r.CreatedAtUtc),
            include: q => q
                .Include(r => r.Category)
                .Include(r => r.HostUser)
                .Include(r => r.Participants),
            cancellationToken: cancellationToken);

    public Task<bool> JoinCodeExistsAsync(string joinCode, CancellationToken cancellationToken = default)
        => AnyAsync(r => r.JoinCode == joinCode, cancellationToken);

    public Task<Room?> GetActiveRoomForUserAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
        => Context.Rooms
            .AsNoTracking()
            .Include(r => r.Category)
            .Where(r => (r.Status == RoomStatus.Waiting || r.Status == RoomStatus.InProgress)
                        && r.Participants.Any(p => p.UserId == userId)
                        // Saati gelmemiş etkinlik kaydı "devam eden oda" sayılmaz.
                        && !(r.IsOfficialEvent
                             && r.Status == RoomStatus.Waiting
                             && r.ScheduledStartUtc > nowUtc))
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Room>> GetUpcomingEventsAsync(
        DateTime nowUtc,
        int take,
        CancellationToken cancellationToken = default)
        => await Context.Rooms
            .AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.HostUser)
            .Include(r => r.Participants)
            .Where(r => r.IsOfficialEvent
                        && r.Status == RoomStatus.Waiting
                        && r.ScheduledStartUtc != null
                        && r.ScheduledStartUtc > nowUtc)
            .OrderBy(r => r.ScheduledStartUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<PagedList<Room>> GetEventsForAdminAsync(
        PageRequest pageRequest,
        CancellationToken cancellationToken = default)
        => GetPagedListAsync(
            pageRequest,
            predicate: r => r.IsOfficialEvent,
            // Yaklaşanlar üstte, geçmişler altta: yöneticinin ilgilendiği
            // kayıtlar her zaman ilk sayfada.
            orderBy: q => q.OrderByDescending(r => r.ScheduledStartUtc),
            include: q => q
                .Include(r => r.Category)
                .Include(r => r.HostUser)
                .Include(r => r.Participants),
            cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<Room>> GetDueEventsAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
        => await Context.Rooms
            .Include(r => r.Participants)
            .Where(r => r.IsOfficialEvent
                        && r.Status == RoomStatus.Waiting
                        && r.ScheduledStartUtc != null
                        && r.ScheduledStartUtc <= nowUtc)
            .OrderBy(r => r.ScheduledStartUtc)
            .ToListAsync(cancellationToken);
}

public sealed class EfRoomParticipantRepository
    : EfEntityRepositoryBase<RoomParticipant, QuizArenaDbContext>, IRoomParticipantRepository
{
    public EfRoomParticipantRepository(QuizArenaDbContext context) : base(context) { }

    public Task<RoomParticipant?> GetByRoomAndUserAsync(
        Guid roomId,
        Guid userId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
        => GetAsync(
            p => p.RoomId == roomId && p.UserId == userId,
            include: q => q.Include(p => p.User),
            asNoTracking: asNoTracking,
            cancellationToken: cancellationToken);

    public Task<int> CountByRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
        => CountAsync(p => p.RoomId == roomId, cancellationToken);

    public Task<IReadOnlyList<RoomParticipant>> GetByRoomAsync(
        Guid roomId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
        => GetListAsync(
            p => p.RoomId == roomId,
            orderBy: q => q.OrderBy(p => p.JoinOrder),
            include: q => q.Include(p => p.User),
            asNoTracking: asNoTracking,
            cancellationToken: cancellationToken);
}

public sealed class EfCompetitionRepository
    : EfEntityRepositoryBase<Competition, QuizArenaDbContext>, ICompetitionRepository
{
    public EfCompetitionRepository(QuizArenaDbContext context) : base(context) { }

    public Task<Competition?> GetByParticipantAsync(
        Guid roomParticipantId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
        => GetAsync(
            c => c.RoomParticipantId == roomParticipantId,
            asNoTracking: asNoTracking,
            cancellationToken: cancellationToken);

    public Task<Competition?> GetActiveForUserAsync(
        Guid userId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Competition> query = Context.Competitions
            .Include(c => c.Room)
                .ThenInclude(r => r.Category)
            .Include(c => c.RoomParticipant)
            .Where(c => c.Status == CompetitionStatus.InProgress
                        && c.RoomParticipant.UserId == userId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query
            .OrderByDescending(c => c.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScoreboardRow>> GetScoreboardAsync(
        Guid roomId,
        CancellationToken cancellationToken = default)
        => await Context.Competitions
            .AsNoTracking()
            .Where(c => c.RoomId == roomId)
            .OrderByDescending(c => c.TotalScore)
            .ThenBy(c => c.FinishedAtUtc)
            .Select(c => new ScoreboardRow(
                c.RoomParticipant.UserId,
                c.RoomParticipant.User.Nickname,
                c.RoomParticipant.User.AvatarUrl,
                c.TotalScore,
                c.CorrectCount,
                c.Status == CompetitionStatus.Completed))
            .ToListAsync(cancellationToken);

    public Task<PagedList<Competition>> GetHistoryForUserAsync(
        Guid userId,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default)
        => GetPagedListAsync(
            pageRequest,
            predicate: c => c.RoomParticipant.UserId == userId
                            && c.Status == CompetitionStatus.Completed,
            orderBy: q => q.OrderByDescending(c => c.FinishedAtUtc),
            include: q => q.Include(c => c.Room).ThenInclude(r => r.Category),
            cancellationToken: cancellationToken);

    public Task<int> CountByRoomAndStatusAsync(
        Guid roomId,
        CompetitionStatus status,
        CancellationToken cancellationToken = default)
        => CountAsync(c => c.RoomId == roomId && c.Status == status, cancellationToken);
}

public sealed class EfCompetitionQuestionRepository
    : EfEntityRepositoryBase<CompetitionQuestion, QuizArenaDbContext>, ICompetitionQuestionRepository
{
    public EfCompetitionQuestionRepository(QuizArenaDbContext context) : base(context) { }

    public Task<CompetitionQuestion?> GetNextUnansweredAsync(
        Guid competitionId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<CompetitionQuestion> query = Context.CompetitionQuestions
            .Include(cq => cq.Question)
                .ThenInclude(q => q.Answers)
            .Include(cq => cq.Question)
                .ThenInclude(q => q.Category)
            // Cevabı olmayan ilk soru = sıradaki soru.
            .Where(cq => cq.CompetitionId == competitionId && cq.Answer == null);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.OrderBy(cq => cq.Order).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<CompetitionQuestion?> GetForAnsweringAsync(
        Guid competitionQuestionId,
        CancellationToken cancellationToken = default)
        // Bilinçli olarak izlemeli (tracked): cevap kaydedilirken aynı bağlamda
        // yarışma skoru da güncellenecek.
        => Context.CompetitionQuestions
            .Include(cq => cq.Question)
                .ThenInclude(q => q.Answers)
            .Include(cq => cq.Answer)
            .Include(cq => cq.Competition)
                .ThenInclude(c => c.RoomParticipant)
            .FirstOrDefaultAsync(cq => cq.Id == competitionQuestionId, cancellationToken);

    public Task<int> CountAnsweredAsync(Guid competitionId, CancellationToken cancellationToken = default)
        => CountAsync(cq => cq.CompetitionId == competitionId && cq.Answer != null, cancellationToken);
}

public sealed class EfCompetitionAnswerRepository
    : EfEntityRepositoryBase<CompetitionAnswer, QuizArenaDbContext>, ICompetitionAnswerRepository
{
    public EfCompetitionAnswerRepository(QuizArenaDbContext context) : base(context) { }
}
