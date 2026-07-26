using QuizArena.Core.Entities.Concrete;
using QuizArena.DAL.ReadModels;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Dtos.Admin;
using QuizArena.Entities.Dtos.Categories;
using QuizArena.Entities.Dtos.Play;
using QuizArena.Entities.Dtos.Questions;
using QuizArena.Entities.Dtos.Rooms;
using QuizArena.Entities.Dtos.Statistics;
using QuizArena.Entities.Dtos.Users;
using QuizArena.Entities.Enums;

namespace QuizArena.BLL.Mapping;

/// <summary>
/// Varlık → DTO dönüşümleri.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden AutoMapper gibi bir kütüphane kullanılmadı?</b>
/// </para>
/// <list type="bullet">
///   <item>
///     <b>Güvenlik:</b> Bu projedeki en kritik kural "doğru cevap ve parola
///     özeti istemciye sızmayacak". Yansımayla otomatik eşleme yapan bir
///     kütüphanede yeni bir alan eklendiğinde <b>sessizce</b> eşlenip yanıta
///     girebilir. Elle yazılan eşlemede bir alanın yanıta girmesi için
///     birinin onu bilinçli olarak yazması gerekir.
///   </item>
///   <item>
///     <b>Derleme zamanı güvence:</b> DTO'ya alan eklenince eşleme derlenmez;
///     hata çalışma zamanına değil derleyiciye düşer.
///   </item>
///   <item>
///     <b>Performans:</b> Yansıma yok, sözlük araması yok, başlangıç maliyeti yok.
///   </item>
/// </list>
/// <para>
/// Maliyeti: birkaç düzine satır sıkıcı kod. Kazanımı: yanıtın içinde ne
/// olduğunun tek tek bilinmesi.
/// </para>
/// </remarks>
public static class EntityMappingExtensions
{
    // ---------------------------------------------------------------------
    //  Kullanıcı
    // ---------------------------------------------------------------------
    public static UserProfileResponse ToProfileResponse(this User user, IReadOnlyList<string> roles) => new(
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.Nickname,
        user.AvatarUrl,
        user.City,
        user.BirthDate,
        user.CreatedAtUtc,
        user.LastLoginAtUtc,
        roles);

    public static UserSummaryResponse ToSummaryResponse(this User user) =>
        new(user.Id, user.Nickname, user.AvatarUrl);

    public static AdminUserResponse ToAdminResponse(this User user, IReadOnlyList<string> roles) => new(
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.Nickname,
        user.IsActive,
        user.AccessFailedCount,
        user.LockoutEndUtc,
        user.LastLoginAtUtc,
        user.CreatedAtUtc,
        roles);

    // ---------------------------------------------------------------------
    //  Kategori
    // ---------------------------------------------------------------------
    public static CategoryResponse ToResponse(this CategoryWithQuestionCount source) => new(
        source.Id,
        source.Name,
        source.Slug,
        source.Description,
        source.Icon,
        source.ColorHex,
        source.IsActive,
        source.DisplayOrder,
        source.ActiveQuestionCount);

    public static CategoryResponse ToResponse(this Category category, int questionCount) => new(
        category.Id,
        category.Name,
        category.Slug,
        category.Description,
        category.Icon,
        category.ColorHex,
        category.IsActive,
        category.DisplayOrder,
        questionCount);

    // ---------------------------------------------------------------------
    //  Soru (YÖNETİM görünümü — doğru cevap bilgisi içerir)
    // ---------------------------------------------------------------------
    public static QuestionResponse ToAdminResponse(this Question question) => new(
        question.Id,
        question.CategoryId,
        question.Category?.Name ?? string.Empty,
        question.Text,
        question.Difficulty,
        question.TimeLimitSeconds,
        question.Explanation,
        question.IsActive,
        question.TimesAsked,
        question.TimesAnsweredCorrectly,
        question.Answers
            .OrderBy(a => a.DisplayOrder)
            .Select(a => new QuestionAnswerResponse(a.Id, a.Text, a.IsCorrect, a.DisplayOrder))
            .ToArray());

    // ---------------------------------------------------------------------
    //  Oda
    // ---------------------------------------------------------------------
    /// <param name="includeJoinCode">
    /// Katılım kodu yalnızca odanın içindeki kişilere gösterilir. Bu bayrağın
    /// varlığı bir güvenlik kararıdır: eşleme fonksiyonu "kime gösteriyorum?"
    /// sorusunu çağırana sordurur, varsayılan olarak kodu sızdırmaz.
    /// </param>
    public static RoomResponse ToResponse(this Room room, bool includeJoinCode) => new(
        room.Id,
        room.Name,
        includeJoinCode ? room.JoinCode : null,
        room.CategoryId,
        room.Category?.Name ?? string.Empty,
        room.Category?.Icon,
        room.Mode,
        room.Status,
        room.QuestionCount,
        room.SecondsPerQuestion,
        room.MaxPlayers,
        room.HostUserId,
        room.HostUser?.Nickname ?? string.Empty,
        room.CreatedAtUtc,
        room.StartedAtUtc,
        room.Participants
            .OrderBy(p => p.JoinOrder)
            .Select(p => p.ToResponse())
            .ToArray());

    public static RoomParticipantResponse ToResponse(this RoomParticipant participant) => new(
        participant.UserId,
        participant.User?.Nickname ?? string.Empty,
        participant.User?.AvatarUrl,
        participant.Role,
        participant.IsReady,
        participant.JoinOrder,
        participant.TotalScore);

    public static RoomSummaryResponse ToSummaryResponse(this Room room) => new(
        room.Id,
        room.Name,
        room.Category?.Name ?? string.Empty,
        room.Category?.Icon,
        room.Mode,
        room.Status,
        room.QuestionCount,
        room.SecondsPerQuestion,
        room.Participants.Count,
        room.MaxPlayers,
        room.HostUser?.Nickname ?? string.Empty,
        room.CreatedAtUtc);

    /// <summary>
    /// Odayı etkinlik yanıtına çevirir.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Katılım kodu bilinçli olarak yok.</b> Etkinlik zaten herkese açık
    /// listeleniyor; kodu göstermek, kayıt kontrolünü (kontenjan, saat)
    /// atlatarak doğrudan odaya girmenin yolunu açardı.
    /// </para>
    /// <para>
    /// <c>CanRegister</c> kararı burada, yani <b>sunucuda</b> veriliyor.
    /// Aynı kuralı arayüzde tekrar yazmak, iki tarafın er ya da geç ayrışması
    /// demek: sunucu "kontenjan dolu" derken düğme hâlâ etkin görünürdü.
    /// </para>
    /// </remarks>
    public static EventResponse ToEventResponse(this Room room, Guid? currentUserId, DateTime nowUtc)
    {
        bool isRegistered = currentUserId is not null
                            && room.Participants.Any(p => p.UserId == currentUserId);

        bool canRegister = !isRegistered
                           && room.Status == RoomStatus.Waiting
                           && room.ScheduledStartUtc > nowUtc
                           && room.Participants.Count < room.MaxPlayers;

        return new EventResponse(
            room.Id,
            room.Name,
            room.Description,
            room.CategoryId,
            room.Category?.Name ?? string.Empty,
            room.Category?.Icon,
            room.Status,
            room.ScheduledStartUtc ?? room.CreatedAtUtc,
            room.QuestionCount,
            room.SecondsPerQuestion,
            room.MaxPlayers,
            room.Participants.Count,
            room.HostUser?.Nickname ?? string.Empty,
            room.CreatedAtUtc,
            room.StartedAtUtc,
            isRegistered,
            canRegister);
    }

    // ---------------------------------------------------------------------
    //  Oyun
    // ---------------------------------------------------------------------
    /// <summary>
    /// Soruyu oyuncuya sunulabilir hâle getirir.
    /// </summary>
    /// <remarks>
    /// <b>Dikkat:</b> Şıklar <see cref="QuizOptionResponse"/> tipine
    /// dönüştürülüyor ve o tipte <c>IsCorrect</c> alanı <b>yok</b>. Doğru
    /// cevabın istemciye sızmaması bir <c>if</c> kontrolüne değil, tipin
    /// yapısına bağlanmış durumda.
    /// </remarks>
    public static QuizQuestionResponse ToQuizResponse(
        this CompetitionQuestion competitionQuestion,
        int totalQuestions,
        int currentScore,
        int currentStreak,
        DateTime closesAtUtc)
    {
        Question question = competitionQuestion.Question;

        return new QuizQuestionResponse(
            competitionQuestion.Id,
            competitionQuestion.Order,
            totalQuestions,
            question.Text,
            question.Category?.Name ?? string.Empty,
            question.Difficulty,
            question.TimeLimitSeconds,
            closesAtUtc,
            currentScore,
            currentStreak,
            question.Answers
                .OrderBy(a => a.DisplayOrder)
                .Select(a => new QuizOptionResponse(a.Id, a.Text))
                .ToArray());
    }

    public static ScoreboardEntryResponse ToResponse(this ScoreboardRow row, int rank) => new(
        rank,
        row.UserId,
        row.Nickname,
        row.AvatarUrl,
        row.TotalScore,
        row.CorrectCount,
        row.IsFinished);

    public static CompetitionHistoryResponse ToHistoryResponse(this Competition competition) => new(
        competition.Id,
        competition.Room?.Category?.Name ?? string.Empty,
        competition.Room?.Category?.Icon,
        competition.TotalScore,
        competition.QuestionCount,
        competition.CorrectCount,
        competition.AccuracyPercentage,
        competition.LongestStreak,
        CalculateDurationSeconds(competition),
        competition.FinishedAtUtc ?? competition.StartedAtUtc);

    public static int CalculateDurationSeconds(this Competition competition) =>
        competition.FinishedAtUtc is null
            ? 0
            : (int)Math.Round((competition.FinishedAtUtc.Value - competition.StartedAtUtc).TotalSeconds);

    // ---------------------------------------------------------------------
    //  İstatistik / rozet
    // ---------------------------------------------------------------------
    public static UserStatisticResponse ToResponse(
        this UserStatistic statistic,
        User user,
        IReadOnlyList<AchievementResponse> achievements) => new(
        statistic.UserId,
        user.Nickname,
        user.AvatarUrl,
        statistic.TotalCompetitions,
        statistic.TotalQuestionsAnswered,
        statistic.TotalCorrectAnswers,
        statistic.TotalScore,
        statistic.AccuracyPercentage,
        statistic.BestStreak,
        statistic.BestCompetitionScore,
        statistic.WinCount,
        statistic.AverageAnswerMilliseconds,
        statistic.LastPlayedAtUtc,
        achievements);

    public static LeaderboardEntryResponse ToResponse(this LeaderboardRow row, int rank) => new(
        rank,
        row.UserId,
        row.Nickname,
        row.AvatarUrl,
        row.TotalScore,
        row.TotalCompetitions,
        row.TotalQuestionsAnswered == 0
            ? 0
            : Math.Round(row.TotalCorrectAnswers * 100d / row.TotalQuestionsAnswered, 1),
        row.BestStreak);

    public static AchievementResponse ToResponse(this Achievement achievement, DateTime? earnedAtUtc = null) => new(
        achievement.Code,
        achievement.Name,
        achievement.Description,
        achievement.Icon,
        achievement.RewardPoints,
        earnedAtUtc);

    public static AchievementResponse ToResponse(this UserAchievement userAchievement) =>
        userAchievement.Achievement.ToResponse(userAchievement.EarnedAtUtc);

    // ---------------------------------------------------------------------
    //  Yetki
    // ---------------------------------------------------------------------
    public static OperationClaimResponse ToResponse(this OperationClaim claim) =>
        new(claim.Id, claim.Name, claim.Description);
}
