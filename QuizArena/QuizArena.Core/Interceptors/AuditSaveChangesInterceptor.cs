using QuizArena.Core.Entities;
using QuizArena.Core.Utilities.Clock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace QuizArena.Core.Interceptors;

/// <summary>
/// <c>SaveChanges</c> anında denetim (audit) alanlarını otomatik dolduran
/// EF Core kesici (interceptor).
/// </summary>
/// <remarks>
/// <para>
/// Alternatifi, her servis metodunda <c>entity.CreatedAtUtc = DateTime.UtcNow</c>
/// yazmaktır. Bu satır <b>bir yerde unutulur</b> — ve unutulduğu satırlar
/// veritabanında <c>0001-01-01</c> tarihiyle durur. Kesici, kuralı tek yerde
/// ve kaçış olmadan uygular.
/// </para>
/// <para>
/// Ayrıca <c>CreatedAtUtc</c> güncellemelerde <b>değiştirilemez</b> hâle
/// getirilir: bir istemci gövdede eski oluşturma tarihini gönderip kaydın
/// geçmişini yeniden yazamaz.
/// </para>
/// </remarks>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IClock _clock;

    public AuditSaveChangesInterceptor(IClock clock) => _clock = clock;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DateTime now = _clock.UtcNow;

        foreach (EntityEntry<IAuditable> entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    entry.Entity.UpdatedAtUtc = null;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = now;
                    // Oluşturma tarihi geçmişe dönük değiştirilemez.
                    entry.Property(e => e.CreatedAtUtc).IsModified = false;
                    break;
            }
        }
    }
}
