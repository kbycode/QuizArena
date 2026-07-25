namespace QuizArena.Core.Exceptions;

/// <summary>
/// Kaynağın mevcut durumu istenen işlemle çelişiyor. → HTTP 409.
/// Örnek: aynı e-posta ile ikinci kayıt, dolu odaya katılma.
/// </summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}
