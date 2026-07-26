using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.Core.DataAccess.Paging;
using QuizArena.Entities.Dtos.Rooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuizArena.Api.Controllers;

/// <summary>Zamanlanmış etkinlikler (turnuvalar).</summary>
/// <remarks>
/// <para>
/// İki tür uç var ve yetkileri farklı:
/// </para>
/// <list type="bullet">
///   <item>
///     <b>Oyuncu uçları</b> (<c>upcoming</c>, <c>register</c>, <c>withdraw</c>)
///     — oturum açmış herkese açık.
///   </item>
///   <item>
///     <b>Yönetim uçları</b> (liste, oluştur, güncelle, iptal) —
///     <c>Admin</c> veya <c>Event.Manage</c>.
///   </item>
/// </list>
/// <para>
/// Etkinliği başlatan uç <b>yoktur</b>: başlatma kararını saat verir ve
/// arka plan hizmeti uygular. HTTP üzerinden erken başlatma yolu açmak,
/// ilan edilen saate güveni ortadan kaldırırdı.
/// </para>
/// </remarks>
[Route("api/[controller]")]
[Authorize]
public sealed class EventsController : ApiControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService) => _eventService = eventService;

    // =====================================================================
    //  Oyuncu uçları
    // =====================================================================

    /// <summary>Yaklaşan etkinlikler.</summary>
    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming(CancellationToken cancellationToken)
        => FromResult(await _eventService.GetUpcomingAsync(cancellationToken));

    /// <summary>Etkinliğe kaydolur.</summary>
    [HttpPost("{eventId:guid}/register")]
    public async Task<IActionResult> Register(Guid eventId, CancellationToken cancellationToken)
        => FromResult(await _eventService.RegisterAsync(eventId, cancellationToken));

    /// <summary>Etkinlik kaydını geri alır.</summary>
    [HttpDelete("{eventId:guid}/register")]
    public async Task<IActionResult> Withdraw(Guid eventId, CancellationToken cancellationToken)
        => FromResult(await _eventService.WithdrawAsync(eventId, cancellationToken));

    // =====================================================================
    //  Yönetim uçları
    // =====================================================================

    /// <summary>Tüm etkinlikler (her durumdaki, sayfalı).</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.EventManage}")]
    public async Task<IActionResult> GetForAdmin(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
        => FromResult(await _eventService.GetForAdminAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            cancellationToken));

    /// <summary>Yeni etkinlik oluşturur.</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.EventManage}")]
    public async Task<IActionResult> Create(
        [FromBody] SaveEventRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _eventService.CreateAsync(request, cancellationToken));

    /// <summary>Başlamamış bir etkinliği günceller.</summary>
    [HttpPut("{eventId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.EventManage}")]
    public async Task<IActionResult> Update(
        Guid eventId,
        [FromBody] SaveEventRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _eventService.UpdateAsync(eventId, request, cancellationToken));

    /// <summary>Etkinliği iptal eder.</summary>
    [HttpPost("{eventId:guid}/cancel")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.EventManage}")]
    public async Task<IActionResult> Cancel(Guid eventId, CancellationToken cancellationToken)
        => FromResult(await _eventService.CancelAsync(eventId, cancellationToken));
}
