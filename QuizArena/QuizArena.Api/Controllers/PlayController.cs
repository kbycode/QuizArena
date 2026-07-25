using QuizArena.BLL.Abstract;
using QuizArena.Core.DataAccess.Paging;
using QuizArena.Entities.Dtos.Play;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuizArena.Api.Controllers;

/// <summary>Yarışma oynama akışı.</summary>
/// <remarks>
/// <para>
/// Akış: <c>GET current</c> → <c>POST answer</c> → (tekrar) →
/// <c>GET summary/{competitionId}</c>
/// </para>
/// <para>
/// Bu uçların hiçbiri "hangi yarışma?" bilgisini istemciden almaz; oturum
/// açmış kullanıcının devam eden yarışması sunucuda bulunur. Böylece bir
/// oyuncu başkasının yarışmasına müdahale edemez.
/// </para>
/// </remarks>
[Route("api/[controller]")]
[Authorize]
public sealed class PlayController : ApiControllerBase
{
    private readonly IGameService _gameService;

    public PlayController(IGameService gameService) => _gameService = gameService;

    /// <summary>Cevaplanacak sıradaki soruyu getirir.</summary>
    /// <remarks>
    /// <para>
    /// Yanıt <b>doğru cevabı içermez</b>; şıklar yalnızca kimlik ve metinden
    /// oluşur. Süre sayacı sunucunun döndürdüğü <c>closesAtUtc</c> değerine
    /// göre çalışır.
    /// </para>
    /// <para>
    /// Soru kalmadıysa <c>data: null</c> döner — istemci sonuç ekranına geçer.
    /// </para>
    /// </remarks>
    /// <response code="200">Soru veya yarışmanın bittiğini gösteren boş veri.</response>
    /// <response code="400">Devam eden yarışma yok.</response>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentQuestion(CancellationToken cancellationToken)
        => FromResult(await _gameService.GetCurrentQuestionAsync(cancellationToken));

    /// <summary>Cevabı gönderir.</summary>
    /// <remarks>
    /// Geçen süre isteğe <b>dahil edilmez</b>: sunucu, soruyu sunduğu andan
    /// itibaren süreyi kendisi ölçer. <c>selectedAnswerId</c> boş bırakılırsa
    /// soru pas geçilmiş sayılır.
    /// </remarks>
    /// <response code="200">Cevap kaydedildi; doğru cevap ve puan dökümü döner.</response>
    /// <response code="403">Bu yarışma size ait değil.</response>
    /// <response code="409">Bu soruyu zaten cevapladınız.</response>
    [HttpPost("answer")]
    public async Task<IActionResult> SubmitAnswer(
        [FromBody] SubmitAnswerRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _gameService.SubmitAnswerAsync(request, cancellationToken));

    /// <summary>Yarışma sonuç ekranı (skor tablosu ve kazanılan rozetlerle).</summary>
    [HttpGet("summary/{competitionId:guid}")]
    public async Task<IActionResult> GetSummary(Guid competitionId, CancellationToken cancellationToken)
        => FromResult(await _gameService.GetSummaryAsync(competitionId, cancellationToken));

    /// <summary>Devam eden yarışmayı yarıda bırakır.</summary>
    /// <remarks>
    /// Yarıda bırakılan yarışma istatistiklere <b>işlenmez</b>: aksi hâlde
    /// "kötü gidiyor" diyerek çıkmak, doğruluk oranını korumanın bir yolu olurdu.
    /// </remarks>
    [HttpPost("abandon")]
    public async Task<IActionResult> Abandon(CancellationToken cancellationToken)
        => FromResult(await _gameService.AbandonAsync(cancellationToken));

    /// <summary>Tamamlanmış yarışmaların geçmişi.</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
        => FromResult(await _gameService.GetMyHistoryAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            cancellationToken));
}
