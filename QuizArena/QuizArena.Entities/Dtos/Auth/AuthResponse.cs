using QuizArena.Core.Entities;
using QuizArena.Entities.Dtos.Users;

namespace QuizArena.Entities.Dtos.Auth;

/// <summary>
/// Başarılı kimlik doğrulama yanıtı.
/// </summary>
/// <remarks>
/// Projenin ilk hâlinde <c>AuthController.Login</c> doğrudan
/// <c>AccessToken</c> nesnesini dönüyordu ve istemci kullanıcı bilgisini
/// alabilmek için ayrı bir istek atmak zorundaydı. Burada jeton + profil tek
/// yanıtta veriliyor; ancak profil DTO'su <b>hiçbir koşulda</b> parola
/// özeti/tuzu içermiyor.
/// </remarks>
public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    UserProfileResponse User) : IDto;
