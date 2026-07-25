using System.Globalization;
using System.Text;
using System.Text.Json;
using QuizArena.Core.CrossCuttingConcerns.Caching;
using QuizArena.Core.Utilities.Results;
using QuizArena.Core.Utilities.Security;
using Microsoft.Extensions.DependencyInjection;

namespace QuizArena.Core.Aspects.Caching;

/// <summary>
/// Metodun dönüşünü önbellekler; sonraki çağrılarda hedef metot hiç çalışmaz.
/// </summary>
/// <remarks>
/// <para>
/// Kullanım: <c>[CacheAspect(DurationMinutes = 30, KeyPrefix = "Category")]</c>
/// </para>
/// <para>
/// <b>Bu uygulamanın naif örneklerden farkları:</b>
/// </para>
/// <list type="bullet">
///   <item>
///     <b>Sonuç gerçekten beklenir.</b> Yaygın örneklerde <c>OnSuccess</c>,
///     <c>Task</c> henüz tamamlanmadan çalışır ve önbelleğe <c>Task</c> nesnesi
///     ya da eksik veri yazılır. <see cref="AspectInterceptor"/> sonucu
///     <c>await</c> ettiği için burada gerçek değer saklanır.
///   </item>
///   <item>
///     <b>Başarısız sonuç önbelleklenmez.</b> <see cref="IResult.Success"/>
///     <c>false</c> ise saklanmaz — aksi hâlde geçici bir hata dakikalarca
///     "dondurulmuş hata" olarak servis edilirdi.
///   </item>
///   <item>
///     <b><see cref="VaryByUser"/>.</b> Kullanıcıya özel veri döndüren bir metot
///     önbelleklenirken kullanıcı kimliği anahtara katılmazsa <b>bir kullanıcının
///     verisi başkasına servis edilir</b>. Bu, önbellek kullanımında en sık
///     yapılan güvenlik hatasıdır; burada açıkça işaretlenmesi zorunlu kılınmıştır.
///   </item>
/// </list>
/// </remarks>
public sealed class CacheAspect : AspectAttribute
{
    private const string CacheKeyState = "__cacheKey";
    private const int MaxKeyLength = 512;

    private static readonly JsonSerializerOptions KeyJsonOptions = new()
    {
        WriteIndented = false
    };

    public CacheAspect()
    {
        // Doğrulamadan sonra, transaction'dan önce: isabetli önbellekte
        // transaction hiç açılmaz.
        Order = 20;
    }

    public int DurationMinutes { get; init; } = 10;

    /// <summary>
    /// Anahtar öneki. <c>CacheRemoveAspect</c> ile aynı öneki kullanan tüm
    /// girdiler tek hamlede temizlenir.
    /// </summary>
    public string? KeyPrefix { get; init; }

    /// <summary>
    /// <c>true</c> ise anahtara oturum açmış kullanıcının kimliği eklenir.
    /// Kullanıcıya özel veri döndüren metotlarda <b>zorunlu</b>.
    /// </summary>
    public bool VaryByUser { get; init; }

    public override void OnBefore(AspectContext context)
    {
        var cache = context.Services.GetRequiredService<ICacheService>();

        string key = BuildKey(context);
        context.State[CacheKeyState] = key;

        if (cache.TryGet<object>(key, out object? cached) && cached is not null)
        {
            // Hedef metot çağrılmayacak; değer doğrudan dönecek.
            context.ShortCircuit(cached);
        }
    }

    public override ValueTask OnSuccessAsync(AspectContext context)
    {
        // Zaten önbellekten geldiyse tekrar yazmanın anlamı yok.
        if (context.ShortCircuited || context.ReturnValue is null)
        {
            return ValueTask.CompletedTask;
        }

        // Başarısız iş sonuçları önbelleklenmez.
        if (context.ReturnValue is IResult { Success: false })
        {
            return ValueTask.CompletedTask;
        }

        if (context.State.TryGetValue(CacheKeyState, out object? keyObject) && keyObject is string key)
        {
            context.Services
                .GetRequiredService<ICacheService>()
                .Set(key, context.ReturnValue, TimeSpan.FromMinutes(DurationMinutes));
        }

        return ValueTask.CompletedTask;
    }

    private string BuildKey(AspectContext context)
    {
        var builder = new StringBuilder(128);

        builder.Append(KeyPrefix ?? context.Method.DeclaringType?.Name ?? "Global");
        builder.Append(':').Append(context.Method.Name);

        if (VaryByUser)
        {
            Guid? userId = context.Services.GetService<ICurrentUserService>()?.UserId;
            builder.Append(":u=").Append(userId?.ToString() ?? "anon");
        }

        if (context.Arguments.Length > 0)
        {
            builder.Append(':').Append(SerializeArguments(context.Arguments));
        }

        string key = builder.ToString();

        // Aşırı uzun anahtarlar sözlükte yer israfıdır; kısaltıp deterministik
        // bir özet ekliyoruz (çakışma riski pratikte yok).
        return key.Length <= MaxKeyLength
            ? key
            : string.Concat(
                key.AsSpan(0, MaxKeyLength),
                "#",
                key.GetHashCode(StringComparison.Ordinal).ToString("x8", CultureInfo.InvariantCulture));
    }

    private static string SerializeArguments(object?[] arguments)
    {
        try
        {
            return JsonSerializer.Serialize(arguments, KeyJsonOptions);
        }
        catch (NotSupportedException)
        {
            // Serileştirilemeyen argüman (ör. delege, döngüsel referans):
            // anahtar için ToString() yeterli.
            return string.Join('|', arguments.Select(a => a?.ToString() ?? "null"));
        }
    }
}
