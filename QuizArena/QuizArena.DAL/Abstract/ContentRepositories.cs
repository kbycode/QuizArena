using QuizArena.Core.DataAccess;
using QuizArena.DAL.ReadModels;
using QuizArena.Entities.Concrete;

namespace QuizArena.DAL.Abstract;

public interface ICategoryRepository : IEntityRepository<Category>
{
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kategorileri, her birinin yarışmaya uygun soru sayısıyla birlikte
    /// <b>tek sorguda</b> döner.
    /// </summary>
    Task<IReadOnlyList<CategoryWithQuestionCount>> GetWithQuestionCountsAsync(
        bool onlyActive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Slug'ın kullanımda olup olmadığını, <b>yumuşak silinmiş kayıtlar
    /// dahil</b> kontrol eder.
    /// </summary>
    /// <remarks>
    /// Global sorgu filtresi silinmiş kategorileri gizler; ancak veritabanındaki
    /// tekil indeks onları <b>görmeye devam eder</b>. Normal bir sorguyla
    /// kontrol yapılsaydı şu senaryo mümkün olurdu: "Tarih" kategorisi
    /// oluşturulur → silinir → yeniden "Tarih" oluşturulmak istenir → kontrol
    /// "slug boş" der → veritabanı tekil indeks ihlaliyle 500 hatası verir.
    /// Bu yüzden kontrol <c>IgnoreQueryFilters()</c> ile yapılır.
    /// </remarks>
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Kategori adının kullanımda olup olmadığı (silinmişler dahil).</summary>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

public interface IQuestionRepository : IEntityRepository<Question>
{
    /// <summary>Soruyu şıklarıyla birlikte getirir.</summary>
    Task<Question?> GetWithAnswersAsync(
        Guid id,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kategorideki yarışmaya uygun soruların <b>yalnızca kimliklerini</b> döner.
    /// </summary>
    /// <remarks>
    /// <b>Neden "rastgele N soru" doğrudan SQL'de seçilmiyor?</b>
    /// SQL Server'da <c>ORDER BY NEWID()</c> tüm tabloyu tarayıp sıralamaya
    /// zorlar; ayrıca <c>RAND()</c> bir sorguda tüm satırlar için aynı değeri
    /// üretir, yani gerçek karıştırma yapmaz. Sağlayıcıya özgü SQL yazmak da
    /// SQLite ile çalışan testleri kırar.
    /// Bu yüzden yalnızca kimlikler (16 bayt/satır) çekilip karıştırma iş
    /// katmanında yapılır; ardından seçilen sorular tek sorguda şıklarıyla
    /// yüklenir. Hem sağlayıcıdan bağımsız hem de test edilebilir.
    /// </remarks>
    Task<IReadOnlyList<Guid>> GetSelectableQuestionIdsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>Verilen kimliklere ait soruları şıklarıyla getirir.</summary>
    Task<IReadOnlyList<Question>> GetWithAnswersByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soru istatistiklerini artırır (kaç kez soruldu / kaç kez doğru bilindi).
    /// </summary>
    /// <remarks>
    /// Varlığı çekip güncellemek yerine tek <c>UPDATE</c> cümlesi çalıştırır
    /// (<c>ExecuteUpdate</c>). Eşzamanlı yarışmalarda aynı sorunun sayacı
    /// güncellenirken "son yazan kazanır" (lost update) sorununu da önler:
    /// artırma işlemi veritabanında atomik olarak yapılır.
    /// </remarks>
    Task IncrementAskedStatisticsAsync(
        IReadOnlyCollection<Guid> questionIds,
        CancellationToken cancellationToken = default);

    Task IncrementCorrectStatisticAsync(
        Guid questionId,
        CancellationToken cancellationToken = default);
}

public interface IAnswerRepository : IEntityRepository<Answer>
{
    Task<IReadOnlyList<Answer>> GetByQuestionAsync(
        Guid questionId,
        CancellationToken cancellationToken = default);

    /// <summary>Soruya ait tüm şıkları siler (soru güncellenirken kullanılır).</summary>
    Task DeleteByQuestionAsync(Guid questionId, CancellationToken cancellationToken = default);
}
