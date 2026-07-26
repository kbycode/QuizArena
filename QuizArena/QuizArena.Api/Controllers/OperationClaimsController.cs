using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.Entities.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuizArena.Api.Controllers;

/// <summary>Yetki tanımları ve kullanıcı-yetki atamaları.</summary>
/// <remarks>
/// <para>
/// <b>Yetki dağıtabilmek, kendine yetki verebilmek demektir.</b> Korumasız
/// bir yetki atama ucu, tek istekle tam yönetici olunabilen bir kapıdır; bu
/// yüzden controller'ın tamamı yalnızca <c>Admin</c> rolüne açıktır.
/// <c>User.Manage</c> hesabı açıp kapatmaya yeter, yetki dağıtmaya yetmez.
/// </para>
/// <para>
/// Kısıt <b>iki katmanda</b> tanımlı: burada
/// <c>[Authorize(Roles = Admin)]</c>, iş metodunda
/// <c>[SecuredOperationAspect(Roles.Admin)]</c>. Ek olarak sistem yetkileri
/// silinemez/yeniden adlandırılamaz ve yönetici kendi <c>Admin</c> yetkisini
/// kaldıramaz — ikisi de sisteme erişimin kalıcı kaybını önler.
/// </para>
/// </remarks>
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
public sealed class OperationClaimsController : ApiControllerBase
{
    private readonly IOperationClaimService _operationClaimService;

    public OperationClaimsController(IOperationClaimService operationClaimService)
        => _operationClaimService = operationClaimService;

    /// <summary>Tanımlı yetkiler.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
        => FromResult(await _operationClaimService.GetListAsync(cancellationToken));

    /// <summary>Yeni yetki tanımı oluşturur.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveOperationClaimRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _operationClaimService.CreateAsync(request, cancellationToken));

    /// <summary>Yetki tanımını günceller.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] SaveOperationClaimRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _operationClaimService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Yetki tanımını kaldırır (sistem yetkileri korunur).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => FromResult(await _operationClaimService.DeleteAsync(id, cancellationToken));

    /// <summary>Kullanıcıya yetki atar.</summary>
    [HttpPost("assign")]
    public async Task<IActionResult> Assign(
        [FromBody] AssignOperationClaimRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _operationClaimService.AssignToUserAsync(request, cancellationToken));

    /// <summary>Kullanıcıdan yetki kaldırır.</summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(
        [FromBody] AssignOperationClaimRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _operationClaimService.RevokeFromUserAsync(request, cancellationToken));
}
