namespace QuizArena.Core.Utilities.Results;

/// <summary>
/// Başarısız, veri taşımayan sonuç.
/// </summary>
public sealed class ErrorResult : Result
{
    public ErrorResult() : base(false) { }

    public ErrorResult(string message) : base(false, message) { }
}
