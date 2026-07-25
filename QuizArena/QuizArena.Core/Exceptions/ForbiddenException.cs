namespace QuizArena.Core.Exceptions;

/// <summary>
/// Kimlik doğrulandı ama bu işlem için yetki yok. → HTTP 403.
/// (401 ile farkı: 401 "kim olduğunu bilmiyorum", 403 "biliyorum ama olmaz".)
/// </summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Bu işlem için yetkiniz yok.")
        : base(message) { }
}
