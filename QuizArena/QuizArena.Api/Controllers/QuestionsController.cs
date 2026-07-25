using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.Core.DataAccess.Paging;
using QuizArena.Entities.Dtos.Questions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuizArena.Api.Controllers;

/// <summary>Soru yönetimi (yalnızca yetkili kullanıcılar).</summary>
/// <remarks>
/// <para>
/// <b>Bu controller'ın tamamı yetki gerektirir ve bu bir tercih değil
/// zorunluluktur:</b> döndürdüğü <see cref="QuestionResponse"/> her şıkkın
/// doğru olup olmadığını içerir. Yetkisiz erişilebilir olsaydı bir oyuncu
/// yarışma öncesi tüm cevap anahtarını indirebilirdi.
/// </para>
/// <para>
/// Orijinal projede <c>QuestionsController</c> ve <c>AnswersController</c>
/// tamamen açıktı; <c>GET /api/answers/getall</c> çağrısı her şıkkın
/// <c>isTrue</c> değerini döndürüyordu. Ayrı bir <c>AnswersController</c>
/// artık yok: şıklar sorunun bir parçası olarak, soruyla aynı işlemde
/// yönetiliyor — böylece şıksız (cevaplanamaz) soru oluşturmak imkânsız.
/// </para>
/// </remarks>
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.Admin},{Roles.QuestionManage}")]
public sealed class QuestionsController : ApiControllerBase
{
    private readonly IQuestionService _questionService;

    public QuestionsController(IQuestionService questionService) => _questionService = questionService;

    /// <summary>Soru listesi (sayfalı, kategoriye göre filtrelenebilir).</summary>
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] Guid? categoryId = null,
        CancellationToken cancellationToken = default)
        => FromResult(await _questionService.GetPagedAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            categoryId,
            cancellationToken));

    /// <summary>Tek soru (şıklarıyla).</summary>
    [HttpGet("{id:guid}", Name = nameof(GetQuestionById))]
    public async Task<IActionResult> GetQuestionById(Guid id, CancellationToken cancellationToken)
        => FromResult(await _questionService.GetByIdAsync(id, cancellationToken));

    /// <summary>Soruyu ve şıklarını tek işlemde oluşturur.</summary>
    /// <remarks>
    /// Şıklarda <b>tam olarak bir</b> doğru cevap bulunmalıdır; doğrulama bunu
    /// zorunlu kılar. Aksi hâlde yarışmada çıkan soru ya hiç bilinemez ya da
    /// seçime göre farklı sonuç verir.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _questionService.CreateAsync(request, cancellationToken);

        return CreatedFromResult(result, nameof(GetQuestionById), new { id = result.Data!.Id });
    }

    /// <summary>Soruyu ve şıklarını günceller.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateQuestionRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _questionService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Soruyu kaldırır (yumuşak silme; geçmiş yarışmalar korunur).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => FromResult(await _questionService.DeleteAsync(id, cancellationToken));
}
