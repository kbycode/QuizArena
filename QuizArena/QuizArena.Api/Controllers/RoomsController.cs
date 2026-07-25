using QuizArena.BLL.Abstract;
using QuizArena.Core.DataAccess.Paging;
using QuizArena.Entities.Dtos.Rooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuizArena.Api.Controllers;

/// <summary>Yarışma odaları: kur, katıl, hazırlan, başlat.</summary>
[Route("api/[controller]")]
[Authorize]
public sealed class RoomsController : ApiControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService) => _roomService = roomService;

    /// <summary>Katılıma açık genel odalar.</summary>
    [HttpGet]
    public async Task<IActionResult> GetJoinable(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
        => FromResult(await _roomService.GetJoinableAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            cancellationToken));

    /// <summary>
    /// Kullanıcının devam eden odası. Sayfa yenilendiğinde oyuna geri dönmek için.
    /// </summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyActiveRoom(CancellationToken cancellationToken)
        => FromResult(await _roomService.GetMyActiveRoomAsync(cancellationToken));

    /// <summary>Oda ayrıntısı.</summary>
    /// <remarks>
    /// Katılım kodu yalnızca odanın katılımcılarına döner; dışarıdan bakan
    /// biri kodu göremez.
    /// </remarks>
    [HttpGet("{roomId:guid}", Name = nameof(GetRoomById))]
    public async Task<IActionResult> GetRoomById(Guid roomId, CancellationToken cancellationToken)
        => FromResult(await _roomService.GetAsync(roomId, cancellationToken));

    /// <summary>Oda kurar.</summary>
    /// <remarks>
    /// <c>Solo</c> kipinde yarışma aynı işlemde başlatılır; istemcinin ayrıca
    /// "başlat" çağırması gerekmez.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateRoomRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _roomService.CreateAsync(request, cancellationToken);

        return CreatedFromResult(result, nameof(GetRoomById), new { roomId = result.Data!.Id });
    }

    /// <summary>Katılım koduyla odaya girer.</summary>
    [HttpPost("join")]
    public async Task<IActionResult> Join(
        [FromBody] JoinRoomRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _roomService.JoinAsync(request, cancellationToken));

    /// <summary>Odadan ayrılır (kurucu ayrılamaz, iptal edebilir).</summary>
    [HttpPost("{roomId:guid}/leave")]
    public async Task<IActionResult> Leave(Guid roomId, CancellationToken cancellationToken)
        => FromResult(await _roomService.LeaveAsync(roomId, cancellationToken));

    /// <summary>"Hazırım" işaretini değiştirir.</summary>
    [HttpPatch("{roomId:guid}/ready")]
    public async Task<IActionResult> SetReady(
        Guid roomId,
        [FromQuery] bool isReady = true,
        CancellationToken cancellationToken = default)
        => FromResult(await _roomService.SetReadyAsync(roomId, isReady, cancellationToken));

    /// <summary>Yarışmayı başlatır (yalnızca odayı kuran kişi).</summary>
    /// <remarks>
    /// Tüm katılımcılara <b>aynı sorular aynı sırayla</b> verilir; aksi hâlde
    /// skorlar karşılaştırılabilir olmazdı.
    /// </remarks>
    [HttpPost("{roomId:guid}/start")]
    public async Task<IActionResult> Start(Guid roomId, CancellationToken cancellationToken)
        => FromResult(await _roomService.StartAsync(roomId, cancellationToken));

    /// <summary>Odayı iptal eder (yalnızca odayı kuran kişi).</summary>
    [HttpPost("{roomId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid roomId, CancellationToken cancellationToken)
        => FromResult(await _roomService.CancelAsync(roomId, cancellationToken));
}
