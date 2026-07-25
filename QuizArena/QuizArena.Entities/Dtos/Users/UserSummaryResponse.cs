using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Users;

/// <summary>
/// Kullanıcının <b>herkese açık</b> görünümü: sıralama tablosu, oda
/// katılımcı listesi, skor tablosu.
/// </summary>
/// <remarks>
/// E-posta, şehir, doğum tarihi <b>bilinçli olarak yok</b>. Bir oyuncu diğer
/// oyuncuların kişisel verisini görmek zorunda değil; veri minimizasyonu
/// (KVKK md. 4) tam olarak budur.
/// </remarks>
public sealed record UserSummaryResponse(
    Guid Id,
    string Nickname,
    string? AvatarUrl) : IDto;
