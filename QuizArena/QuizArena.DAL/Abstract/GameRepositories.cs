using QuizArena.Core.DataAccess;
using QuizArena.Core.DataAccess.Paging;
using QuizArena.DAL.ReadModels;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Enums;

namespace QuizArena.DAL.Abstract;

public interface IRoomRepository : IEntityRepository<Room>
{
    Task<Room?> GetByJoinCodeAsync(
        string joinCode,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Odayı kategori, kurucu ve katılımcı kullanıcılarıyla birlikte getirir.
    /// Tek sorguda yüklenir; oda ekranı için ayrıca sorgu atmak gerekmez.
    /// </summary>
    Task<Room?> GetDetailAsync(
        Guid id,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    /// <summary>Katılıma açık genel odalar (sayfalı).</summary>
    Task<PagedList<Room>> GetJoinableRoomsAsync(
        PageRequest pageRequest,
        CancellationToken cancellationToken = default);

    Task<bool> JoinCodeExistsAsync(string joinCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının şu anda içinde bulunduğu, bitmemiş oda.
    /// </summary>
    /// <remarks>
    /// Başlama saati gelmemiş <b>etkinlik kayıtları hariçtir</b>. Aksi hâlde
    /// üç gün sonraki bir turnuvaya kaydolan oyuncu, o güne kadar hiçbir oyun
    /// oynayamaz ve arayüz onu sürekli "devam eden odanız var" diye boş bir
    /// lobiye yönlendirirdi.
    /// </remarks>
    Task<Room?> GetActiveRoomForUserAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    // --- Zamanlanmış etkinlikler --------------------------------------------

    /// <summary>Yaklaşan (henüz başlamamış) resmî etkinlikler.</summary>
    Task<IReadOnlyList<Room>> GetUpcomingEventsAsync(
        DateTime nowUtc,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>Yönetim listesi: her durumdaki etkinlikler (sayfalı).</summary>
    Task<PagedList<Room>> GetEventsForAdminAsync(
        PageRequest pageRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Başlama saati gelmiş, hâlâ beklemedeki etkinlikler.
    /// </summary>
    /// <remarks>
    /// Arka plan hizmeti bunu düzenli aralıklarla çağırır. Katılımcılar da
    /// yüklenir: hizmet, kimin başlatılacağına karar vermek için onlara bakar.
    /// </remarks>
    Task<IReadOnlyList<Room>> GetDueEventsAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
}

public interface IRoomParticipantRepository : IEntityRepository<RoomParticipant>
{
    Task<RoomParticipant?> GetByRoomAndUserAsync(
        Guid roomId,
        Guid userId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<int> CountByRoomAsync(Guid roomId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoomParticipant>> GetByRoomAsync(
        Guid roomId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);
}

public interface ICompetitionRepository : IEntityRepository<Competition>
{
    Task<Competition?> GetByParticipantAsync(
        Guid roomParticipantId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının devam eden yarışması (varsa).</summary>
    Task<Competition?> GetActiveForUserAsync(
        Guid userId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    /// <summary>Odadaki tüm katılımcıların anlık skor tablosu.</summary>
    Task<IReadOnlyList<ScoreboardRow>> GetScoreboardAsync(
        Guid roomId,
        CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının tamamlanmış yarışma geçmişi (sayfalı).</summary>
    Task<PagedList<Competition>> GetHistoryForUserAsync(
        Guid userId,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default);

    Task<int> CountByRoomAndStatusAsync(
        Guid roomId,
        CompetitionStatus status,
        CancellationToken cancellationToken = default);
}

public interface ICompetitionQuestionRepository : IEntityRepository<CompetitionQuestion>
{
    /// <summary>
    /// Yarışmadaki sıradaki cevaplanmamış soruyu, şıkları ve kategorisiyle getirir.
    /// </summary>
    Task<CompetitionQuestion?> GetNextUnansweredAsync(
        Guid competitionId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cevap doğrulaması için soruyu; şıkları, yarışması ve katılımcısıyla getirir.
    /// </summary>
    /// <remarks>
    /// Bu metot <b>izlemeli (tracked)</b> çalışır: cevap kaydedilirken yarışma
    /// skoru da aynı bağlamda güncellenecek.
    /// </remarks>
    Task<CompetitionQuestion?> GetForAnsweringAsync(
        Guid competitionQuestionId,
        CancellationToken cancellationToken = default);

    Task<int> CountAnsweredAsync(Guid competitionId, CancellationToken cancellationToken = default);
}

public interface ICompetitionAnswerRepository : IEntityRepository<CompetitionAnswer>;
