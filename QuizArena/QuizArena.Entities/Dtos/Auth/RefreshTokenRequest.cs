using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Auth;

/// <summary>Erişim jetonunu yenileme isteği.</summary>
public sealed record RefreshTokenRequest(string RefreshToken) : IDto;
