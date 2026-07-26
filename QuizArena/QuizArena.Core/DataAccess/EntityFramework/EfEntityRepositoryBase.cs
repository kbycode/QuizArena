using System.Linq.Expressions;
using QuizArena.Core.DataAccess.Paging;
using QuizArena.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace QuizArena.Core.DataAccess.EntityFramework;

/// <summary>
/// <see cref="IEntityRepository{TEntity}"/>'nin EF Core uygulaması.
/// Tüm somut repository'ler bundan türer ve tek satır kod yazmaz.
/// </summary>
/// <remarks>
/// <para>
/// <b>DbContext dışarıdan verilir</b> — DI ile, istek başına <c>Scoped</c>.
/// Repository kendi bağlamını yaratmaz. Her metodun
/// <c>using (var context = new TContext())</c> ile kendi bağlamını açması
/// üç somut soruna yol açardı:
/// </para>
/// <list type="number">
///   <item>
///     <b>Transaction imkânsız hâle gelir.</b> İki repository çağrısı iki ayrı
///     bağlantı kullandığında "ikisi birlikte olsun ya da hiçbiri olmasın"
///     garantisi verilemez; yarışma bitirme akışı yarım kalabilir.
///   </item>
///   <item>
///     <b>Bağlantı dizesi koda gömülür.</b> <c>OnConfiguring</c> içine sabit
///     yazılan sunucu adı, uygulamanın başka bir makinede veya ortamda
///     yapılandırılmasını imkânsız kılar.
///   </item>
///   <item>
///     <b>Kimlik (identity) çakışır.</b> Bir metotta çekilen varlık başka bir
///     metodun bağlamında "yabancı" olur; <c>Update</c> sırasında izlenmeyen
///     varlık hataları ve gereksiz tam-satır güncellemeleri oluşur.
///   </item>
/// </list>
/// </remarks>
public abstract class EfEntityRepositoryBase<TEntity, TContext> : IEntityRepository<TEntity>
    where TEntity : class, IEntity
    where TContext : DbContext
{
    protected EfEntityRepositoryBase(TContext context) => Context = context;

    protected TContext Context { get; }

    protected DbSet<TEntity> Entities => Context.Set<TEntity>();

    public async Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = Entities;

        if (include is not null)
        {
            query = include(query);
        }

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int? take = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = BuildQuery(predicate, orderBy, include, asNoTracking);

        if (take is > 0)
        {
            query = query.Take(take.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<PagedList<TEntity>> GetPagedListAsync(
        PageRequest pageRequest,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pageRequest);

        // Sayım sorgusu Include/OrderBy içermez: ikisi de toplam sayıyı
        // değiştirmez ama sorguyu gereksiz yere pahalılaştırır.
        IQueryable<TEntity> countQuery = Entities;
        if (predicate is not null)
        {
            countQuery = countQuery.Where(predicate);
        }

        int totalCount = await countQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return PagedList<TEntity>.Empty(pageRequest);
        }

        List<TEntity> items = await BuildQuery(predicate, orderBy, include, asNoTracking)
            .Skip(pageRequest.Skip)
            .Take(pageRequest.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<TEntity>(items, pageRequest.Page, pageRequest.PageSize, totalCount);
    }

    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => Entities.AsNoTracking().AnyAsync(predicate, cancellationToken);

    public Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        => predicate is null
            ? Entities.AsNoTracking().CountAsync(cancellationToken)
            : Entities.AsNoTracking().CountAsync(predicate, cancellationToken);

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await Entities.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        await Entities.AddRangeAsync(entities, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Varlık zaten bu context tarafından izleniyorsa Update() çağırmak
        // gereksiz — hatta tüm alanları "değişti" işaretleyip UPDATE cümlesini
        // şişirir. Yalnızca izlenmeyen (detached) varlıkta ekliyoruz.
        if (Context.Entry(entity).State == EntityState.Detached)
        {
            Entities.Update(entity);
        }

        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(TEntity entity, bool hardDelete = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!hardDelete && entity is ISoftDeletable softDeletable)
        {
            softDeletable.IsDeleted = true;
            softDeletable.DeletedAtUtc = DateTime.UtcNow;

            if (Context.Entry(entity).State == EntityState.Detached)
            {
                Entities.Update(entity);
            }
        }
        else
        {
            Entities.Remove(entity);
        }

        await Context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<TEntity> BuildQuery(
        Expression<Func<TEntity, bool>>? predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include,
        bool asNoTracking)
    {
        IQueryable<TEntity> query = Entities;

        if (include is not null)
        {
            query = include(query);
        }

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        return query;
    }
}
