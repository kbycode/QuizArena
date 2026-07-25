using QuizArena.Core.DataAccess.Paging;
using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Common;

/// <summary>Sayfalanmış liste yanıtı.</summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPrevious,
    bool HasNext) : IDto
{
    public static PagedResponse<T> From(PagedList<T> source) => new(
        source.Items,
        source.Page,
        source.PageSize,
        source.TotalCount,
        source.TotalPages,
        source.HasPrevious,
        source.HasNext);
}
