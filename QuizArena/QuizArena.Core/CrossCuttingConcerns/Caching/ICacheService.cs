namespace QuizArena.Core.CrossCuttingConcerns.Caching;

/// <summary>
/// Önbellek soyutlaması. Bugün <c>IMemoryCache</c> ile karşılanıyor;
/// yarın uygulama birden fazla sunucuya dağıtıldığında yalnızca bu arayüzün
/// Redis uygulaması yazılır, iş katmanında tek satır değişmez.
/// </summary>
public interface ICacheService
{
    bool TryGet<T>(string key, out T? value);

    void Set<T>(string key, T value, TimeSpan duration);

    /// <summary>Değer yoksa üretir, önbelleğe koyar ve döner.</summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    void Remove(string key);

    /// <summary>
    /// Belirtilen önekle başlayan tüm anahtarları siler.
    /// Örnek: bir soru güncellenince <c>"Question:"</c> önekli her şey düşer.
    /// </summary>
    void RemoveByPrefix(string prefix);
}
