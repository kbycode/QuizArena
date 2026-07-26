using QuizArena.Entities.Enums;

namespace QuizArena.DAL.ReadModels;

/// <summary>
/// Veri katmanına özel, hafif okuma modelleri (projeksiyonlar).
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden varlık değil, neden API DTO'su da değil?</b>
/// </para>
/// <list type="bullet">
///   <item>
///     Varlık döndürmek gereksiz kolon çeker ve <c>COUNT</c> gibi türetilmiş
///     değerleri taşıyamaz; sonuçta BLL her kategori için ayrı sayım sorgusu
///     atmak zorunda kalır (N+1 problemi).
///   </item>
///   <item>
///     Doğrudan API DTO'su döndürmek ise veri katmanını sunum sözleşmesine
///     bağlar: API yanıtına bir alan eklendiğinde SQL sorgusu değişmek
///     zorunda kalır.
///   </item>
/// </list>
/// <para>
/// Ara katman olarak bu okuma modelleri, tek SQL sorgusuyla ihtiyaç duyulan
/// veriyi getirir; BLL onu sunum DTO'suna çevirir.
/// </para>
/// </remarks>
public sealed record CategoryWithQuestionCount(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Icon,
    string? ColorHex,
    bool IsActive,
    int DisplayOrder,
    int ActiveQuestionCount);

/// <summary>Odadaki bir katılımcının anlık skor satırı.</summary>
public sealed record ScoreboardRow(
    Guid UserId,
    string Nickname,
    string? AvatarUrl,
    int TotalScore,
    int CorrectCount,
    bool IsFinished);

/// <summary>Sıralama tablosu satırı (istatistik + kullanıcı birleşimi).</summary>
public sealed record LeaderboardRow(
    Guid UserId,
    string Nickname,
    string? AvatarUrl,
    int TotalScore,
    int TotalCompetitions,
    int TotalQuestionsAnswered,
    int TotalCorrectAnswers,
    int BestStreak);

// ---------------------------------------------------------------------------
//  Yönetim panosu (dashboard)
// ---------------------------------------------------------------------------

/// <summary>
/// Panonun tek seferde okunan sayaçları.
/// </summary>
/// <remarks>
/// Alanların hepsi ayrı ayrı sorgulanabilirdi; tek kayıtta toplanmasının
/// nedeni <b>çağıran tarafın sözleşmesini sabitlemek</b>. Böylece BLL,
/// veri katmanının kaç sorgu attığını bilmek zorunda kalmaz ve ileride
/// sorgular tek bir birleşik SQL'e indirgense bile arayüz değişmez.
/// </remarks>
public sealed record DashboardCounters(
    int TotalUsers,
    int ActiveUsers,
    int NewUsersInWindow,
    int LockedUsers,
    int TotalCategories,
    int ActiveCategories,
    int TotalQuestions,
    int ActiveQuestions,
    int FinishedCompetitions,
    int CompetitionsInWindow,
    int LiveRooms,
    long TotalAnswers,
    long TotalCorrectAnswers);

/// <summary>Bir günün etkinlik özeti.</summary>
public sealed record DailyActivityRow(DateTime Date, int Competitions, int Players);

/// <summary>Kategori kırılımı: soru sayısı, oynanma ve doğruluk.</summary>
public sealed record CategoryBreakdownRow(
    Guid CategoryId,
    string Name,
    string? Icon,
    string? ColorHex,
    bool IsActive,
    int QuestionCount,
    int CompetitionCount,
    long TimesAsked,
    long TimesAnsweredCorrectly);

/// <summary>Soru başarı istatistiği (en zor / en kolay listeleri için).</summary>
public sealed record QuestionStatisticRow(
    Guid QuestionId,
    string Text,
    string CategoryName,
    QuestionDifficulty Difficulty,
    int TimesAsked,
    int TimesAnsweredCorrectly);
