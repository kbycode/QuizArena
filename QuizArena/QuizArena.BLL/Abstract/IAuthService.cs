using QuizArena.Core.Utilities.Results;
using QuizArena.Entities.Dtos.Auth;

namespace QuizArena.BLL.Abstract;

/// <summary>Kimlik doğrulama ve oturum yönetimi.</summary>
public interface IAuthService
{
    Task<IDataResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<IDataResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Yenileme jetonuyla yeni bir erişim jetonu üretir ve <b>yenileme jetonunu
    /// döndürür (rotasyon)</b>: kullanılan jeton iptal edilir.
    /// </summary>
    Task<IDataResult<AuthResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Verilen yenileme jetonunu iptal eder.</summary>
    Task<IResult> LogoutAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Parolayı değiştirir ve kullanıcının <b>tüm</b> oturumlarını kapatır.
    /// </summary>
    Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);
}
