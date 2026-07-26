namespace QuizArena.Core.DataAccess.Paging;

/// <summary>
/// Sayfalama isteği. Sınırlar sınıfın içinde zorlanır.
/// </summary>
/// <remarks>
/// Sayfalama <b>zorunludur</b>. Tabloyu tümüyle çeken bir uç, tablo
/// büyüdüğünde yalnızca yavaşlamaz; tek istekle yüz binlerce satırı belleğe
/// alarak <b>hizmet dışı bırakma (DoS)</b> yüzeyi açar. İstemcinin
/// <c>pageSize=1000000</c> göndererek aynı etkiyi yaratmasını engellemek için
/// üst sınır burada, DTO'ya güvenmeden uygulanır.
/// </remarks>
public sealed record PageRequest
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;

    private readonly int _page = 1;
    private readonly int _pageSize = DefaultPageSize;

    /// <summary>1'den başlar. Daha küçük değer verilirse 1'e çekilir.</summary>
    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    /// <summary>1..<see cref="MaxPageSize"/> aralığına kırpılır.</summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    public int Skip => (Page - 1) * PageSize;
}
