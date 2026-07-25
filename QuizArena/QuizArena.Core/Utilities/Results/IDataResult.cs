namespace QuizArena.Core.Utilities.Results;

/// <summary>
/// Veri de taşıyan sonuç. <c>out T</c> (kovaryans) sayesinde
/// <c>IDataResult&lt;AdminUserResponse&gt;</c> bir <c>IDataResult&lt;object&gt;</c>
/// olarak kullanılabilir.
/// </summary>
public interface IDataResult<out T> : IResult
{
    T? Data { get; }
}
