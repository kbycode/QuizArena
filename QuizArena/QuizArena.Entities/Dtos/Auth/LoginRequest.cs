using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Auth;

/// <summary>Oturum açma isteği.</summary>
public sealed record LoginRequest(string Email, string Password) : IDto;
