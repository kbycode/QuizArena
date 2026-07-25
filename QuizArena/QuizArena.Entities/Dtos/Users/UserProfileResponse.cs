using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Users;

/// <summary>
/// Kullanıcının <b>kendi</b> profili. E-posta gibi kişisel veri yalnızca
/// sahibine (veya yöneticiye) döner.
/// </summary>
/// <remarks>
/// <b>Bu DTO'nun varlık sebebi doğrudan bir güvenlik açığının kapatılmasıdır.</b>
/// Projenin ilk hâlinde <c>UsersController</c>, <c>User</c> varlığını olduğu
/// gibi JSON'a çeviriyordu; yanıtta <c>passwordHash</c> ve <c>passwordSalt</c>
/// alanları <b>Base64 hâlinde açıkça yer alıyordu</b>. Yani tek bir
/// <c>GET /api/users/getall</c> çağrısı, tüm kullanıcıların parola özetlerini
/// çevrimdışı kırma denemesine hazır biçimde teslim ediyordu.
/// Varlıklar artık hiçbir uçtan doğrudan dönmüyor.
/// </remarks>
public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Nickname,
    string? AvatarUrl,
    string? City,
    DateOnly? BirthDate,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    IReadOnlyList<string> Roles) : IDto;
