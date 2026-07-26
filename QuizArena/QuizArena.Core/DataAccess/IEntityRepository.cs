using System.Linq.Expressions;
using QuizArena.Core.DataAccess.Paging;
using QuizArena.Core.Entities;
using Microsoft.EntityFrameworkCore.Query;

namespace QuizArena.Core.DataAccess;

/// <summary>
/// Jenerik veri erişim sözleşmesi.
/// </summary>
/// <remarks>
/// <para>
/// Bu sözleşmedeki her karar ve <b>nedeni</b>:
/// </para>
/// <list type="number">
///   <item>
///     <b>Tamamı asenkron.</b> Senkron <c>ToList()</c>, veritabanı yanıtını
///     beklerken thread havuzundan bir iş parçacığını bloke eder. Eşzamanlı
///     yük altında havuz tükenir (thread pool starvation) ve uygulama
///     CPU boşta olmasına rağmen yanıt vermemeye başlar.
///   </item>
///   <item>
///     <b><c>CancellationToken</c> her metotta.</b> Kullanıcı sekmeyi kapatınca
///     sunucu hâlâ ağır sorguyu çalıştırıyorsa bu boşa harcanan kapasitedir.
///   </item>
///   <item>
///     <b><c>include</c> parametresi.</b> İlişkili veri ihtiyacı çağıranın
///     kararıdır. Repository'nin içine sabit <c>Include</c> gömmek ya gereksiz
///     veri çeker ya da N+1 sorgu problemine yol açar.
///   </item>
///   <item>
///     <b><c>asNoTracking</c> varsayılan <c>true</c>.</b> Okuma işlemlerinin
///     büyük çoğunluğunda değişiklik takibi (change tracking) gereksizdir;
///     kapatmak bellek ve CPU tasarrufu sağlar. Güncellenecek varlık
///     çekilirken bilinçli olarak <c>false</c> verilir.
///   </item>
///   <item>
///     <b><see cref="IQueryable{T}"/> dışa sızdırılmaz.</b> Sızsaydı iş katmanı
///     veritabanı sorgusu kurmaya başlar, sorgu mantığı katmanlara dağılır ve
///     "DbContext kapandıktan sonra sorgulama" hataları görülürdü.
///   </item>
/// </list>
/// </remarks>
public interface IEntityRepository<TEntity>
    where TEntity : class, IEntity
{
    Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int? take = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<PagedList<TEntity>> GetPagedListAsync(
        PageRequest pageRequest,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Varlığı siler. <see cref="Entities.ISoftDeletable"/> uygulayan tipler
    /// için varsayılan davranış <b>yumuşak silme</b>dir (kayıt kalır, işaret
    /// konur). Gerçekten fiziksel silme gerekiyorsa
    /// <paramref name="hardDelete"/> bilinçli olarak <c>true</c> verilir.
    /// </summary>
    Task DeleteAsync(TEntity entity, bool hardDelete = false, CancellationToken cancellationToken = default);
}
