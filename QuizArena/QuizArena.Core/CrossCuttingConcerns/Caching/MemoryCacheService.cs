using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace QuizArena.Core.CrossCuttingConcerns.Caching;

/// <summary>
/// <see cref="IMemoryCache"/> tabanlı, süreç içi önbellek.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden yanında bir anahtar kümesi tutuluyor?</b> <c>IMemoryCache</c>
/// anahtarlarını listeleme yeteneği sunmaz; "şu önekle başlayan her şeyi sil"
/// isteği doğrudan karşılanamaz. Bu yüzden yazılan anahtarlar ayrıca
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> içinde izlenir ve girdi
/// önbellekten düştüğünde (<c>PostEvictionCallback</c>) izlemeden de silinir.
/// Böylece sözlük sınırsız büyümez.
/// </para>
/// <para>
/// Tüm işlemler eşzamanlı isteklere karşı güvenlidir; servis <c>Singleton</c>
/// olarak kaydedilir.
/// </para>
/// </remarks>
public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    /// <summary>Aktif anahtarlar (değer kullanılmaz; küme olarak davranır).</summary>
    private readonly ConcurrentDictionary<string, byte> _keys = new(StringComparer.Ordinal);

    /// <summary>
    /// Aynı anahtar için eşzamanlı üretimi tekilleştirir (cache stampede önlemi):
    /// 50 istek aynı anda sıralama tablosunu isterse veritabanına 50 değil
    /// 1 sorgu gider.
    /// </summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public MemoryCacheService(IMemoryCache cache) => _cache = cache;

    public bool TryGet<T>(string key, out T? value)
    {
        if (_cache.TryGetValue(key, out object? cached) && cached is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan duration)
    {
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(duration)
            .RegisterPostEvictionCallback((evictedKey, _, _, _) => _keys.TryRemove((string)evictedKey, out _));

        _cache.Set(key, value, options);
        _keys[key] = 0;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        if (TryGet<T>(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        SemaphoreSlim gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            // Kilidi beklerken başka bir istek doldurmuş olabilir.
            if (TryGet(key, out cached) && cached is not null)
            {
                return cached;
            }

            T produced = await factory(cancellationToken);
            if (produced is not null)
            {
                Set(key, produced, duration);
            }

            return produced;
        }
        finally
        {
            gate.Release();
            _locks.TryRemove(key, out _);
        }
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
    }

    public void RemoveByPrefix(string prefix)
    {
        foreach (string key in _keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
        {
            Remove(key);
        }
    }
}
