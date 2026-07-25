using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Users;

/// <summary>
/// Yönetici panelinde görünen kullanıcı kaydı.
/// Hesap durumu ve kilit bilgisi içerir; <b>parola özeti ve güvenlik damgası
/// içermez</b> — yöneticinin bile bunları görmesi için hiçbir meşru gerekçe yok.
/// </summary>
public sealed record AdminUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Nickname,
    bool IsActive,
    int AccessFailedCount,
    DateTime? LockoutEndUtc,
    DateTime? LastLoginAtUtc,
    DateTime CreatedAtUtc,
    IReadOnlyList<string> Roles) : IDto;
