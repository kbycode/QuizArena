namespace QuizArena.Core.Exceptions;

/// <summary>Kimlik doğrulanamadı. → HTTP 401.</summary>
public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Kimlik doğrulanamadı.")
        : base(message) { }
}
