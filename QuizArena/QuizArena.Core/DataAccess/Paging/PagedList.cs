namespace QuizArena.Core.DataAccess.Paging;

/// <summary>Sayfalanmış sonuç kümesi + gezinme bilgisi.</summary>
public sealed class PagedList<T>
{
    public PagedList(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public static PagedList<T> Empty(PageRequest request) =>
        new([], request.Page, request.PageSize, 0);

    /// <summary>Öğeleri başka bir tipe çevirir; sayfalama bilgisi korunur.</summary>
    public PagedList<TTarget> Map<TTarget>(Func<T, TTarget> selector) =>
        new(Items.Select(selector).ToArray(), Page, PageSize, TotalCount);
}
