using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Users;

/// <summary>
/// Kullanıcının <b>kendi</b> profili. E-posta gibi kişisel veri yalnızca
/// sahibine (veya yöneticiye) döner.
/// </summary>
/// <remarks>
/// <b>Varlıklar hiçbir uçtan doğrudan dönmez; bu DTO o kuralın gereğidir.</b>
/// <c>User</c> varlığı olduğu gibi JSON'a çevrilseydi yanıtta
/// <c>passwordHash</c> ve <c>passwordSalt</c> alanları Base64 hâlinde yer
/// alır; tek bir liste çağrısı, tüm kullanıcıların parola özetlerini
/// çevrimdışı kırma denemesine hazır biçimde teslim ederdi. Alanlar burada
/// elle seçilir.
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
