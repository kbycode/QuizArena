using QuizArena.Api.Extensions;
using QuizArena.BLL.Abstract;
using QuizArena.Entities.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace QuizArena.Api.Controllers;

/// <summary>Kayıt, giriş, oturum yenileme ve parola işlemleri.</summary>
/// <remarks>
/// <b>Tüm uçlara sıkı hız sınırı uygulanır</b> (<c>[EnableRateLimiting("auth")]</c>).
/// Bu, hesap bazlı kilitlemeyi tamamlar: kilitleme tek bir hesabı korur,
/// hız sınırı "bin farklı hesaba birer deneme" biçimindeki dağıtık parola
/// denemesini (credential stuffing) yavaşlatır.
/// </remarks>
[Route("api/[controller]")]
[EnableRateLimiting(SecurityServiceExtensions.AuthRateLimitPolicy)]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Yeni kullanıcı kaydı oluşturur ve oturum açar.</summary>
    /// <response code="200">Kayıt oluşturuldu; jeton çifti döner.</response>
    /// <response code="400">Doğrulama hatası (zayıf parola, geçersiz e-posta…).</response>
    /// <response code="409">E-posta veya takma ad zaten kullanımda.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _authService.RegisterAsync(request, cancellationToken));

    /// <summary>Oturum açar.</summary>
    /// <response code="200">Giriş başarılı; jeton çifti döner.</response>
    /// <response code="401">E-posta/parola hatalı veya hesap kilitli.</response>
    /// <response code="429">Çok fazla deneme.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _authService.LoginAsync(request, cancellationToken));

    /// <summary>Erişim jetonunu yeniler.</summary>
    /// <remarks>
    /// Yenileme jetonu <b>tek kullanımlıktır</b>: bu çağrı eski jetonu iptal
    /// eder ve yenisini döner (rotasyon). İptal edilmiş bir jeton tekrar
    /// kullanılırsa kullanıcının tüm oturumları güvenlik gereği kapatılır.
    /// </remarks>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _authService.RefreshAsync(request, cancellationToken));

    /// <summary>Verilen yenileme jetonunu iptal eder (çıkış).</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _authService.LogoutAsync(request, cancellationToken));

    /// <summary>Parolayı değiştirir ve tüm oturumları kapatır.</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
        => FromResult(await _authService.ChangePasswordAsync(request, cancellationToken));
}
