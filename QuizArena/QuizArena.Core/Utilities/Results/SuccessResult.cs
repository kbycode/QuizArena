namespace QuizArena.Core.Utilities.Results;

/// <summary>
/// Başarılı, veri taşımayan sonuç.
/// </summary>
public sealed class SuccessResult : Result
{
    public SuccessResult() : base(true) { }

    public SuccessResult(string message) : base(true, message) { }
}
