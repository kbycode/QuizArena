using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuizArena.Api.Controllers;

/// <summary>Yönetim panosu.</summary>
/// <remarks>
/// Yalnızca <c>Admin</c> rolüne açık. Kısıt <b>iki katmanda</b> birden
/// tanımlı: burada <c>[Authorize(Roles = …)]</c>, iş katmanında
/// <c>[SecuredOperationAspect(Roles.Admin)]</c>. Tekrar gibi görünse de
/// bilinçli: controller kontrolü yalnızca HTTP yolunu korur, servis
/// doğrudan (ör. bir arka plan işinden) çağrılsa bile yetki kontrolü
/// yürürlükte kalır.
/// </remarks>
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
public sealed class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
        => _dashboardService = dashboardService;

    /// <summary>Panonun tüm blokları (özet, günlük etkinlik, kategoriler, soru listeleri).</summary>
    /// <param name="windowDays">
    /// "Son N gün" pencere genişliği. İş katmanı bunu
    /// <see cref="GameRules.MinDashboardWindowDays"/> –
    /// <see cref="GameRules.MaxDashboardWindowDays"/> aralığına sıkıştırır;
    /// aralık dışı bir değer hata değil, sınıra çekilmiş bir sonuç üretir.
    /// </param>
    /// <param name="cancellationToken">İstek iptal belirteci.</param>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int windowDays = GameRules.DefaultDashboardWindowDays,
        CancellationToken cancellationToken = default)
        => FromResult(await _dashboardService.GetAsync(windowDays, cancellationToken));
}
