using QuizArena.Core.Entities;
using QuizArena.Entities.Enums;

namespace QuizArena.Entities.Dtos.Rooms;

/// <summary>
/// Açık oda listesi kaydı. Katılım kodu <b>bilinçli olarak yok</b>:
/// listeyi gören herkes koda sahip olsaydı "özel oda" kavramı anlamsızlaşırdı.
/// </summary>
public sealed record RoomSummaryResponse(
    Guid Id,
    string Name,
    string CategoryName,
    string? CategoryIcon,
    RoomMode Mode,
    RoomStatus Status,
    int QuestionCount,
    int SecondsPerQuestion,
    int CurrentPlayerCount,
    int MaxPlayers,
    string HostNickname,
    DateTime CreatedAtUtc) : IDto;
