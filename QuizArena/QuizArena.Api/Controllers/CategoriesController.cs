using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.Entities.Dtos.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuizArena.Api.Controllers;

/// <summary>Soru kategorileri.</summary>
/// <remarks>
/// Okuma uçları <c>[AllowAnonymous]</c>: giriş yapmamış bir ziyaretçi de
/// ana sayfada kategorileri görebilmeli. Yazma uçları rol gerektirir.
/// </remarks>
[Route("api/[controller]")]
public sealed class CategoriesController : ApiControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService) => _categoryService = categoryService;

    /// <summary>Kategori listesi (her birinin yarışmaya uygun soru sayısıyla).</summary>
    /// <param name="includeInactive">
    /// Kapatılmış kategoriler de listelensin mi? Yalnızca yöneticiler için
    /// anlamlı; yetkisiz istekte yok sayılır.
    /// </param>
    /// <param name="cancellationToken">İstek iptal jetonu.</param>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetList(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        // Kapatılmış kategorileri görmek yönetim bilgisidir; istemcinin
        // sorgu parametresine körlemesine güvenmiyoruz.
        bool onlyActive = !includeInactive || !User.IsInRole(Roles.Admin);

        return FromResult(await _categoryService.GetListAsync(onlyActive, cancellationToken));
    }

    /// <summary>Tek kategori.</summary>
    [HttpGet("{id:guid}", Name = nameof(GetCategoryById))]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken cancellationToken)
        => FromResult(await _categoryService.GetByIdAsync(id, cancellationToken));

    /// <summary>Yeni kategori oluşturur.</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.CategoryManage}")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.CreateAsync(request, cancellationToken);

        return CreatedFromResult(result, nameof(GetCategoryById), new { id = result.Data!.Id });
    }

    /// <summary>Kategoriyi günceller.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.CategoryManage}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _categoryService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Kategoriyi kaldırır (yumuşak silme).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.CategoryManage}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => FromResult(await _categoryService.DeleteAsync(id, cancellationToken));
}
