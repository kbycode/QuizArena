using QuizArena.Core.Entities;
using QuizArena.Entities.Enums;

namespace QuizArena.Entities.Dtos.Admin;

/// <summary>
/// Yönetim panosunun tam yanıtı.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden tek uç, beş ayrı uç değil?</b> Pano açılışta beş bloğu birden
/// gösteriyor. Beş ayrı istek, beş kez kimlik doğrulama, beş bağlantı ve
/// istemcide beş ayrı yükleniyor durumu demekti; blokların bir kısmı gelip
/// bir kısmı gelmediğinde de pano tutarsız görünürdü. Tek uç, panoyu
/// <b>tek bir anın</b> fotoğrafı hâline getiriyor.
/// </para>
/// <para>
/// Yanıt <see cref="GeneratedAtUtc"/> taşıyor: veri önbellekten gelebildiği
/// için arayüz "şu ana ait" demek yerine gerçek üretim anını gösteriyor.
/// </para>
/// </remarks>
public sealed record DashboardResponse(
    DateTime GeneratedAtUtc,
    int WindowDays,
    DashboardSummary Summary,
    IReadOnlyList<DashboardDailyPoint> DailyActivity,
    IReadOnlyList<DashboardCategoryStat> Categories,
    IReadOnlyList<DashboardQuestionStat> HardestQuestions,
    IReadOnlyList<DashboardQuestionStat> EasiestQuestions) : IDto;

/// <summary>Üst şeritteki sayaçlar.</summary>
/// <param name="OverallAccuracy">Tüm zamanların doğruluk oranı (0-100).</param>
public sealed record DashboardSummary(
    int TotalUsers,
    int ActiveUsers,
    int LockedUsers,
    int NewUsersInWindow,
    int TotalCategories,
    int ActiveCategories,
    int TotalQuestions,
    int ActiveQuestions,
    int FinishedCompetitions,
    int CompetitionsInWindow,
    int LiveRooms,
    long TotalAnswers,
    long TotalCorrectAnswers,
    double OverallAccuracy) : IDto;

/// <summary>Günlük etkinlik grafiğinin bir noktası.</summary>
/// <param name="Date">Gün (UTC, saat bileşeni sıfır).</param>
public sealed record DashboardDailyPoint(
    DateTime Date,
    int Competitions,
    int Players) : IDto;

/// <summary>Kategori kırılımı satırı.</summary>
public sealed record DashboardCategoryStat(
    Guid CategoryId,
    string Name,
    string? Icon,
    string? ColorHex,
    bool IsActive,
    int QuestionCount,
    int CompetitionCount,
    long TimesAsked,
    long TimesAnsweredCorrectly,
    double AccuracyPercentage) : IDto;

/// <summary>Soru başarı satırı ("en zor" / "en kolay" listeleri).</summary>
public sealed record DashboardQuestionStat(
    Guid QuestionId,
    string Text,
    string CategoryName,
    QuestionDifficulty Difficulty,
    int TimesAsked,
    int TimesAnsweredCorrectly,
    double SuccessRate) : IDto;
