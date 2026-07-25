namespace QuizArena.Core.Exceptions;

/// <summary>
/// Bir iş kuralı ihlal edildi. → HTTP 400.
/// Örnek: "Yarışma başladıktan sonra odaya katılamazsınız."
/// </summary>
public sealed class BusinessException : AppException
{
    public BusinessException(string message) : base(message) { }
}
