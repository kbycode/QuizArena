using QuizArena.Core.DataAccess;
using QuizArena.DAL.ReadModels;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Enums;

namespace QuizArena.DAL.Abstract;

public interface IUserStatisticRepository : IEntityRepository<UserStatistic>
{
    Task<UserStatistic?> GetByUserAsync(
        Guid userId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Puana göre ilk <paramref name="top"/> oyuncu.
    /// Azalan indeks (<c>IX_UserStatistics_TotalScore_Desc</c>) sayesinde
    /// sıralama işlemi veritabanında ek maliyet doğurmaz.
    /// </summary>
    Task<IReadOnlyList<LeaderboardRow>> GetLeaderboardAsync(
        int top,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının genel sıralamadaki yeri.
    /// "Kendisinden yüksek puanlı kaç kişi var" sayımıyla bulunur; tüm tabloyu
    /// sıralayıp indeks aramaktan çok daha ucuzdur.
    /// </summary>
    Task<int> GetRankAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IAchievementRepository : IEntityRepository<Achievement>
{
    Task<IReadOnlyList<Achievement>> GetByCodesAsync(
        IReadOnlyCollection<AchievementCode> codes,
        CancellationToken cancellationToken = default);
}

public interface IUserAchievementRepository : IEntityRepository<UserAchievement>
{
    /// <summary>Kullanıcının kazandığı rozet kodları (hızlı "zaten var mı?" kontrolü için).</summary>
    Task<IReadOnlyList<AchievementCode>> GetEarnedCodesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserAchievement>> GetByUserWithAchievementAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Belirli bir yarışmada kazanılan rozetler.</summary>
    Task<IReadOnlyList<UserAchievement>> GetByCompetitionAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default);
}
