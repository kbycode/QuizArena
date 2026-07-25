using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuizArena.Api.Controllers;

/// <summary>Genel sıralama tablosu ve rozet kataloğu.</summary>
[Route("api/[controller]")]
public sealed class LeaderboardController : ApiControllerBase
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly IAchievementService _achievementService;

    public LeaderboardController(
        ILeaderboardService leaderboardService,
        IAchievementService achievementService)
    {
        _leaderboardService = leaderboardService;
        _achievementService = achievementService;
    }

    /// <summary>En yüksek puanlı oyuncular.</summary>
    /// <remarks>
    /// Herkese açık: giriş yapmamış ziyaretçi de ana sayfada sıralamayı görür.
    /// Yanıt yalnızca takma ad ve avatar içerir; e-posta gibi kişisel veri
    /// bulunmaz.
    /// <para>
    /// Sonuç 2 dakika önbelleklenir — bu liste her istekte veritabanına
    /// gitmek zorunda olmayacak kadar yavaş değişir.
    /// </para>
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetTop(
        [FromQuery] int top = GameRules.LeaderboardDefaultTop,
        CancellationToken cancellationToken = default)
        => FromResult(await _leaderboardService.GetTopAsync(top, cancellationToken));

    /// <summary>Oturum açmış kullanıcının sıralamadaki yeri (0 = henüz yok).</summary>
    [HttpGet("my-rank")]
    [Authorize]
    public async Task<IActionResult> GetMyRank(CancellationToken cancellationToken)
        => FromResult(await _leaderboardService.GetMyRankAsync(cancellationToken));

    /// <summary>Kazanılabilecek tüm rozetler.</summary>
    [HttpGet("achievements")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAchievementCatalog(CancellationToken cancellationToken)
        => FromResult(await _achievementService.GetCatalogAsync(cancellationToken));
}
