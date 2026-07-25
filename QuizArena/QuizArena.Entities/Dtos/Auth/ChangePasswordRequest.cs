using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Auth;

/// <summary>
/// Parola değiştirme isteği. Mevcut parolanın istenmesi zorunludur:
/// aksi hâlde çalınmış bir erişim jetonuyla hesap kalıcı olarak devralınabilir.
/// </summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword) : IDto;
