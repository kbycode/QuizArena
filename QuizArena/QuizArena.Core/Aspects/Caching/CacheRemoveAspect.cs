using QuizArena.Core.CrossCuttingConcerns.Caching;
using QuizArena.Core.Utilities.Results;
using Microsoft.Extensions.DependencyInjection;

namespace QuizArena.Core.Aspects.Caching;

/// <summary>
/// Metot <b>başarıyla</b> tamamlandıktan sonra belirtilen öneklerle başlayan
/// önbellek girdilerini siler.
/// </summary>
/// <remarks>
/// <para>
/// Kullanım: <c>[CacheRemoveAspect("Category", "Question")]</c>
/// </para>
/// <para>
/// Silme işleminin <c>OnSuccessAsync</c>'te olması kritik: yazma işlemi hata
/// alırsa önbellek <b>geçerli</b> veriyi tutmaya devam eder. Silmeyi
/// <c>OnBefore</c>'da yapmak, başarısız bir güncellemeden sonra önbelleği
/// gereksiz yere soğutur (cache stampede'e davetiye çıkarır).
/// </para>
/// </remarks>
public sealed class CacheRemoveAspect : AspectAttribute
{
    private readonly string[] _prefixes;

    public CacheRemoveAspect(params string[] prefixes)
    {
        if (prefixes is null or { Length: 0 })
        {
            throw new ArgumentException("En az bir önbellek öneki verilmelidir.", nameof(prefixes));
        }

        _prefixes = prefixes;

        // En son çalışsın: transaction commit edildikten sonra temizlensin ki
        // önbellek asla "commit edilmemiş veriye göre" boşaltılmasın.
        Order = 90;
    }

    public override ValueTask OnSuccessAsync(AspectContext context)
    {
        // İş sonucu başarısızsa veri değişmemiştir; önbelleğe dokunmuyoruz.
        if (context.ReturnValue is IResult { Success: false })
        {
            return ValueTask.CompletedTask;
        }

        var cache = context.Services.GetRequiredService<ICacheService>();

        foreach (string prefix in _prefixes)
        {
            cache.RemoveByPrefix(prefix);
        }

        return ValueTask.CompletedTask;
    }
}
