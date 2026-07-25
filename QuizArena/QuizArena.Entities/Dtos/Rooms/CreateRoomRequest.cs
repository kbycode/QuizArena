using QuizArena.Core.Entities;
using QuizArena.Entities.Enums;

namespace QuizArena.Entities.Dtos.Rooms;

/// <summary>
/// Oda kurma isteği. Kurucu kullanıcı istekten değil <b>jetondan</b> okunur —
/// aksi hâlde bir kullanıcı başkası adına oda kurabilirdi.
/// </summary>
public sealed record CreateRoomRequest(
    Guid CategoryId,
    string? Name,
    RoomMode Mode,
    int QuestionCount,
    int SecondsPerQuestion,
    int MaxPlayers) : IDto;
