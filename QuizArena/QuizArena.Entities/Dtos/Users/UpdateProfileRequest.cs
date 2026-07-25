using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Users;

/// <summary>
/// Profil güncelleme isteği.
/// </summary>
/// <remarks>
/// <b>E-posta, rol ve hesap durumu burada yok.</b> Sebebi, aşırı veri gönderimi
/// (over-posting / mass assignment) saldırısıdır: istek gövdesi doğrudan
/// varlığa bağlanırsa kullanıcı gövdeye <c>"isActive": true</c> ya da
/// <c>"roles": ["Admin"]</c> ekleyerek kendini yönetici yapabilir.
/// İstemcinin değiştirmesine izin verilen alanlar bu DTO ile <b>beyaz listeye</b>
/// alınmıştır; listede olmayan hiçbir alan güncellenemez.
/// </remarks>
public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string Nickname,
    string? City,
    DateOnly? BirthDate,
    string? AvatarUrl) : IDto;
