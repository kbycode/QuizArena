using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Rooms;

/// <summary>Katılım kodu ile odaya girme isteği.</summary>
public sealed record JoinRoomRequest(string JoinCode) : IDto;
