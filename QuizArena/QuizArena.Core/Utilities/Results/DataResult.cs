namespace QuizArena.Core.Utilities.Results;

/// <inheritdoc cref="IDataResult{T}"/>
public class DataResult<T> : Result, IDataResult<T>
{
    public DataResult(T? data, bool success, string message = "")
        : base(success, message)
    {
        Data = data;
    }

    public T? Data { get; }
}
