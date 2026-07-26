using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.Core.DataAccess.Paging;
using QuizArena.Entities.Dtos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuizArena.Api.Controllers;

/// <summary>Kullanıcı profili ve (yönetici için) hesap yönetimi.</summary>
/// <remarks>
/// <para>
/// <b>Sistemdeki en hassas veri burada:</b> hesap kayıtları. Üç kural bir
/// arada uygulanır ve her biri somut bir saldırıyı kapatır.
/// </para>
/// <list type="bullet">
///   <item>
///     <b>Sınıf düzeyinde <c>[Authorize]</c>.</b> Kimliği doğrulanmamış hiçbir
///     istek hiçbir uca ulaşamaz.
///   </item>
///   <item>
///     <b>Yönetici uçlarında ayrıca rol kontrolü.</b> Oturum açmış olmak,
///     başkasının hesabını yönetmeye yetmez.
///   </item>
///   <item>
///     <b>Girdi ve çıktıda alanları elle seçilmiş DTO'lar.</b> İstek gövdesi
///     doğrudan <c>User</c> varlığına bağlansaydı, gövdeye
///     <c>passwordHash</c> yazan biri herhangi bir hesabın parolasını
///     değiştirebilirdi (over-posting). Varlık yanıt olarak dönseydi parola
///     özeti ve tuzu Base64 hâlinde istemciye sızardı.
///   </item>
/// </list>
/// </remarks>
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly IStatisticService _statisticService;

    public UsersController(IUserService userService, IStatisticService statisticService)
    {
        _userService = userService;
        _statisticService = statisticService;
    }

    /// <summary>Oturum açmış kullanıcının kendi profili.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
        => FromResult(await _userService.GetMyProfileAsync(cancellationToken));

    /// <summary>Kendi profilini günceller.</summary>
    /// <remarks>
    /// E-posta, rol ve hesap durumu bu uçtan <b>değiştirilemez</b>; istek
    /// gövdesi yalnızca <see cref="UpdateProfileRequest"/> alanlarını taşır.
    /// </remarks>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _userService.UpdateMyProfileAsync(request, cancellationToken));

    /// <summary>Kendi istatistikleri ve rozetleri.</summary>
    [HttpGet("me/statistics")]
    public async Task<IActionResult> GetMyStatistics(CancellationToken cancellationToken)
        => FromResult(await _statisticService.GetMyStatisticsAsync(cancellationToken));

    /// <summary>Başka bir oyuncunun herkese açık bilgileri.</summary>
    /// <remarks>
    /// Yalnızca takma ad ve avatar döner. E-posta, şehir ve doğum tarihi
    /// gibi kişisel veriler <b>bilinçli olarak</b> yer almaz.
    /// </remarks>
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetSummary(Guid userId, CancellationToken cancellationToken)
        => FromResult(await _userService.GetSummaryAsync(userId, cancellationToken));

    /// <summary>Başka bir oyuncunun istatistikleri (profil kartı için).</summary>
    [HttpGet("{userId:guid}/statistics")]
    public async Task<IActionResult> GetStatistics(Guid userId, CancellationToken cancellationToken)
        => FromResult(await _statisticService.GetForUserAsync(userId, cancellationToken));

    // =====================================================================
    //  Yönetici uçları
    // =====================================================================

    /// <summary>Kullanıcı listesi (sayfalı, arama destekli).</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.UserManage}")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
        => FromResult(await _userService.GetPagedAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            search,
            cancellationToken));

    /// <summary>Hesabı etkinleştirir veya devre dışı bırakır.</summary>
    [HttpPatch("{userId:guid}/active")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.UserManage}")]
    public async Task<IActionResult> SetActive(
        Guid userId,
        [FromQuery] bool isActive,
        CancellationToken cancellationToken)
        => FromResult(await _userService.SetActiveAsync(userId, isActive, cancellationToken));

    /// <summary>Hatalı giriş nedeniyle kilitlenmiş hesabın kilidini açar.</summary>
    [HttpPost("{userId:guid}/unlock")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.UserManage}")]
    public async Task<IActionResult> Unlock(Guid userId, CancellationToken cancellationToken)
        => FromResult(await _userService.UnlockAsync(userId, cancellationToken));
}
