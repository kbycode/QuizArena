using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Auth;

/// <summary>Yeni kullanıcı kaydı isteği.</summary>
public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Nickname) : IDto;
