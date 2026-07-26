using QuizArena.Core.Entities;
using QuizArena.Entities.Enums;

namespace QuizArena.Entities.Dtos.Rooms;

/// <summary>
/// Etkinlik oluşturma / güncelleme isteği.
/// </summary>
/// <param name="ScheduledStartUtc">
/// Başlangıç anı — <b>UTC</b>. İstemci kullanıcının yerel saatini gönderirse
/// etkinlik yanlış saatte başlar; bu yüzden alan adı biriminde açık.
/// </param>
public sealed record SaveEventRequest(
    Guid CategoryId,
    string Name,
    string? Description,
    DateTime ScheduledStartUtc,
    int QuestionCount,
    int SecondsPerQuestion,
    int MaxPlayers) : IDto;

/// <summary>
/// Etkinlik kaydı.
/// </summary>
/// <param name="IsRegistered">
/// İsteği yapan kullanıcı kayıtlı mı? Oturum açılmamışsa <c>false</c>.
/// Arayüz "Kaydol" / "Kaydı iptal et" ayrımını buna göre yapar.
/// </param>
/// <param name="CanRegister">
/// Kayıt hâlâ mümkün mü (kontenjan dolmadı, saat geçmedi, durum uygun)?
/// Kararı sunucu veriyor: aynı kuralı istemcide tekrar yazmak, iki tarafın
/// ayrışması demektir.
/// </param>
public sealed record EventResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    string? CategoryIcon,
    RoomStatus Status,
    DateTime ScheduledStartUtc,
    int QuestionCount,
    int SecondsPerQuestion,
    int MaxPlayers,
    int RegisteredCount,
    string HostNickname,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    bool IsRegistered,
    bool CanRegister) : IDto;
