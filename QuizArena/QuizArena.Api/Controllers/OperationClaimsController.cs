using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.Entities.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuizArena.Api.Controllers;

/// <summary>Yetki tanımları ve kullanıcı-yetki atamaları.</summary>
/// <remarks>
/// <para>
/// <b>Bu controller orijinal projede tamamen açıktı.</b> Yani kimliği
/// doğrulanmamış herhangi bir istemci
/// <c>POST /api/useroperationclaims/add</c> çağrısıyla <b>kendisine Admin
/// yetkisi atayabiliyordu</b> — tek istekle tam yönetici olmak mümkündü.
/// </para>
/// <para>
/// Artık hem controller düzeyinde <c>[Authorize(Roles = Admin)]</c> hem de
/// iş katmanında <c>[SecuredOperationAspect(Roles.Admin)]</c> var (katmanlı
/// savunma). Ek olarak sistem yetkileri silinemez/yeniden adlandırılamaz ve
/// yönetici kendi Admin yetkisini kaldıramaz.
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
