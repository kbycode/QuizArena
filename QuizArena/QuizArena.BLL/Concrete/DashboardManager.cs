using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.Core.Aspects.Authorization;
using QuizArena.Core.Aspects.Caching;
using QuizArena.Core.Utilities.Clock;
using QuizArena.Core.Utilities.Results;
using QuizArena.DAL.Abstract;
using QuizArena.DAL.ReadModels;
using QuizArena.Entities.Dtos.Admin;

namespace QuizArena.BLL.Concrete;

/// <summary>Yönetim panosu.</summary>
/// <remarks>
/// <para>
/// Bu servis <b>hiçbir şey yazmaz</b>. Yalnızca okuma modellerini sunum
/// DTO'suna çevirir, oranları hesaplar ve grafiğin boş günlerini doldurur.
/// Bu ayrım bilinçli: oran hesabı veritabanında yapılsaydı sıfıra bölme
/// koruması her sorguda tekrarlanmak zorunda kalırdı; burada tek yerde.
/// </para>
/// <para>
/// <b>Yetki:</b> pano tüm sistemin özetini gösterir — kaç kullanıcı var, kaç
/// hesap kilitli, hangi sorular çalışmıyor. Bu bilgi tek tek uçların
/// yetkilerinin <b>birleşimini</b> gerektirir, o yüzden yalnızca
/// <c>Admin</c>'e açık.
/// </para>
/// </remarks>
public sealed class DashboardManager : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IClock _clock;

    public DashboardManager(IDashboardRepository dashboardRepository, IClock clock)
    {
        _dashboardRepository = dashboardRepository;
        _clock = clock;
    }

    /// <remarks>
    /// <b>Neden önbellek:</b> pano dört toplama sorgusu çalıştırır ve yönetici
    /// sayfayı yenilediğinde hepsi tekrar koşar. Veri dakikalar mertebesinde
    /// anlamlı biçimde değişmediği için kısa süreli önbellek, maliyeti hissedilir
    /// biçimde düşürüyor.
    /// <para>
    /// <c>VaryByUser</c> <b>ayarlanmadı</b> ve bu güvenli: pano yalnızca
    /// <c>Admin</c>'e açık ve içeriği çağırana göre değişmiyor. Kullanıcıya özel
    /// veri dönseydi anahtarın kullanıcı kimliğini içermesi zorunlu olurdu.
    /// </para>
    /// </remarks>
    [SecuredOperationAspect(Roles.Admin)]
    [CacheAspect(KeyPrefix = CacheKeys.Dashboard, DurationMinutes = GameRules.DashboardCacheMinutes)]
    public async Task<IDataResult<DashboardResponse>> GetAsync(
        int windowDays,
        CancellationToken cancellationToken = default)
    {
        int window = Math.Clamp(windowDays, GameRules.MinDashboardWindowDays, GameRules.MaxDashboardWindowDays);

        DateTime nowUtc = _clock.UtcNow;

        // Pencerenin başı gün başına yuvarlanıyor: "son 14 gün" grafiğinin ilk
        // sütunu, isteğin saatine göre yarım gün olmamalı.
        DateTime windowStartUtc = nowUtc.Date.AddDays(-(window - 1));

        DashboardCounters counters =
            await _dashboardRepository.GetCountersAsync(windowStartUtc, nowUtc, cancellationToken);

        IReadOnlyList<DailyActivityRow> activity =
            await _dashboardRepository.GetDailyActivityAsync(windowStartUtc, cancellationToken);

        IReadOnlyList<CategoryBreakdownRow> categories =
            await _dashboardRepository.GetCategoryBreakdownAsync(cancellationToken);

        IReadOnlyList<QuestionStatisticRow> hardest =
            await _dashboardRepository.GetQuestionsBySuccessRateAsync(
                GameRules.DashboardQuestionListSize,
                ascending: true,
                GameRules.DashboardMinTimesAsked,
                cancellationToken);

        IReadOnlyList<QuestionStatisticRow> easiest =
            await _dashboardRepository.GetQuestionsBySuccessRateAsync(
                GameRules.DashboardQuestionListSize,
                ascending: false,
                GameRules.DashboardMinTimesAsked,
                cancellationToken);

        var response = new DashboardResponse(
            GeneratedAtUtc: nowUtc,
            WindowDays: window,
            Summary: BuildSummary(counters),
            DailyActivity: FillMissingDays(activity, windowStartUtc, window),
            Categories: categories.Select(ToStat).ToArray(),
            HardestQuestions: hardest.Select(ToStat).ToArray(),
            EasiestQuestions: easiest.Select(ToStat).ToArray());

        return new SuccessDataResult<DashboardResponse>(response);
    }

    private static DashboardSummary BuildSummary(DashboardCounters counters)
        => new(
            counters.TotalUsers,
            counters.ActiveUsers,
            counters.LockedUsers,
            counters.NewUsersInWindow,
            counters.TotalCategories,
            counters.ActiveCategories,
            counters.TotalQuestions,
            counters.ActiveQuestions,
            counters.FinishedCompetitions,
            counters.CompetitionsInWindow,
            counters.LiveRooms,
            counters.TotalAnswers,
            counters.TotalCorrectAnswers,
            Percentage(counters.TotalCorrectAnswers, counters.TotalAnswers));

    /// <summary>
    /// Grafiğin boş günlerini sıfırla doldurur.
    /// </summary>
    /// <remarks>
    /// Veritabanı yalnızca etkinlik olan günleri döndürür. Bu satırlar
    /// olduğu gibi çizilseydi, hiç oynanmayan günler grafikte <b>hiç
    /// görünmez</b> ve iki uzak tarih yan yana gelerek yanlış bir süreklilik
    /// izlenimi verirdi — "her gün oynanıyor" gibi. Boşlukların açıkça
    /// sıfır olarak çizilmesi, grafiği dürüst kılıyor.
    /// </remarks>
    private static List<DashboardDailyPoint> FillMissingDays(
        IReadOnlyList<DailyActivityRow> rows,
        DateTime windowStartUtc,
        int windowDays)
    {
        Dictionary<DateTime, DailyActivityRow> byDate = rows.ToDictionary(row => row.Date.Date);

        var points = new List<DashboardDailyPoint>(windowDays);

        for (int offset = 0; offset < windowDays; offset++)
        {
            DateTime day = windowStartUtc.AddDays(offset);

            points.Add(byDate.TryGetValue(day, out DailyActivityRow? row)
                ? new DashboardDailyPoint(day, row.Competitions, row.Players)
                : new DashboardDailyPoint(day, 0, 0));
        }

        return points;
    }

    private static DashboardCategoryStat ToStat(CategoryBreakdownRow row)
        => new(
            row.CategoryId,
            row.Name,
            row.Icon,
            row.ColorHex,
            row.IsActive,
            row.QuestionCount,
            row.CompetitionCount,
            row.TimesAsked,
            row.TimesAnsweredCorrectly,
            Percentage(row.TimesAnsweredCorrectly, row.TimesAsked));

    private static DashboardQuestionStat ToStat(QuestionStatisticRow row)
        => new(
            row.QuestionId,
            row.Text,
            row.CategoryName,
            row.Difficulty,
            row.TimesAsked,
            row.TimesAnsweredCorrectly,
            Percentage(row.TimesAnsweredCorrectly, row.TimesAsked));

    /// <summary>Yüzde hesabı — payda sıfırsa 0 döner (tek yerde korunuyor).</summary>
    private static double Percentage(long part, long total)
        => total == 0 ? 0 : Math.Round(part * 100d / total, 1);
}
