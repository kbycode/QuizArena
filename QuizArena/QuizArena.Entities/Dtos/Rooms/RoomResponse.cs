using QuizArena.Core.Entities;
using QuizArena.Entities.Enums;

namespace QuizArena.Entities.Dtos.Rooms;

/// <summary>Oda ayrıntısı (odanın içindeki oyuncular için).</summary>
/// <param name="JoinCode">
/// Katılım kodu. <b>Yalnızca odanın katılımcılarına</b> doldurulur; oda
/// listesinde/dışarıda <c>null</c> döner ki özel odalara davetsiz girilemesin.
/// </param>
public sealed record RoomResponse(
    Guid Id,
    string Name,
    string? JoinCode,
    Guid CategoryId,
    string CategoryName,
    string? CategoryIcon,
    RoomMode Mode,
    RoomStatus Status,
    int QuestionCount,
    int SecondsPerQuestion,
    int MaxPlayers,
    Guid HostUserId,
    string HostNickname,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    IReadOnlyList<RoomParticipantResponse> Participants) : IDto;

/// <summary>Odadaki bir katılımcı.</summary>
public sealed record RoomParticipantResponse(
    Guid UserId,
    string Nickname,
    string? AvatarUrl,
    ParticipantRole Role,
    bool IsReady,
    int JoinOrder,
    int TotalScore) : IDto;
