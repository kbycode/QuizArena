using QuizArena.Core.Entities;
using QuizArena.Entities.Dtos.Users;

namespace QuizArena.Entities.Dtos.Auth;

/// <summary>
/// Başarılı kimlik doğrulama yanıtı.
/// </summary>
/// <remarks>
/// Jeton ve profil <b>tek yanıtta</b> döner; istemcinin kullanıcı bilgisi
/// için ikinci bir istek atması gerekmez. Profil DTO'su <b>hiçbir koşulda</b>
/// parola özeti veya tuzu içermez.
/// </remarks>
public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    UserProfileResponse User) : IDto;
