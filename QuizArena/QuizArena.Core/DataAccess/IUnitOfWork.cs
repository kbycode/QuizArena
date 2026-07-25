namespace QuizArena.Core.DataAccess;

/// <summary>
/// Birden fazla repository çağrısını tek bir atomik işleme (transaction)
/// bağlamak için soyutlama.
/// </summary>
/// <remarks>
/// <para>
/// Somut uygulaması DAL katmanındadır (EF Core'un <c>DbContext</c>'i üzerinden).
/// Core yalnızca sözleşmeyi tanımlar; böylece <c>TransactionAspect</c> hiçbir
/// veritabanı teknolojisi bilmeden çalışır.
/// </para>
/// <para>
/// Neden gerekli: "yarışmayı bitir" işlemi cevabı kaydeder, skoru günceller,
/// istatistiği artırır ve rozet verir. Bunlardan biri hata alırsa hiçbiri
/// gerçekleşmemiş olmalıdır; aksi hâlde veritabanında yarım yarışmalar kalır.
/// </para>
/// <para>
/// <b><see cref="BeginTransaction"/> neden senkron?</b> Bu metodu çağıran
/// <c>TransactionAspect</c>, hedef metoda geçmeden (Castle DynamicProxy'de
/// <c>Proceed()</c>) hemen önce çalışmak zorundadır — arada <c>await</c>
/// olmaması gerekir. EF Core senkron <c>BeginTransaction</c> sunduğu için
/// bu maliyetsizdir. Commit/rollback ise hedef metot beklendikten sonra
/// çalıştığı için asenkron olabilir.
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    bool HasActiveTransaction { get; }

    void BeginTransaction();

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
