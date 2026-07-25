namespace QuizArena.Core.Utilities.Results;

/// <inheritdoc cref="IResult"/>
public class Result : IResult
{
    public Result(bool success, string message = "")
    {
        Success = success;
        Message = message;
    }

    public bool Success { get; }

    public string Message { get; }
}
