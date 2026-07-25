using QuizArena.Core.DataAccess;
using QuizArena.DAL.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace QuizArena.DAL.Concrete.EntityFramework;

/// <summary>
/// <see cref="IUnitOfWork"/>'ün EF Core uygulaması.
/// </summary>
/// <remarks>
/// İstek başına (<c>Scoped</c>) kaydedilir ve aynı <c>DbContext</c> örneğini
/// repository'lerle paylaşır. Paylaşım kritik: transaction bağlantı üzerinde
/// açıldığı için repository'ler farklı bağlam kullanıyor olsaydı
/// transaction onları kapsamazdı.
/// </remarks>
public sealed class EfUnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly QuizArenaDbContext _context;
    private IDbContextTransaction? _transaction;

    public EfUnitOfWork(QuizArenaDbContext context) => _context = context;

    public bool HasActiveTransaction => _transaction is not null;

    public void BeginTransaction()
    {
        if (_transaction is not null)
        {
            // Sessizce yeni bir transaction açmak, dıştakinin erken commit
            // edilmesine yol açar. Çağıran taraf (TransactionAspect) bu durumu
            // HasActiveTransaction ile kontrol ediyor; buraya düşmek bir
            // programlama hatasıdır.
            throw new InvalidOperationException(
                "Zaten açık bir transaction var. İç içe transaction desteklenmiyor.");
        }

        _transaction = _context.Database.BeginTransaction();
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            // Commit öncesi bekleyen değişiklikler yazılır: repository'ler
            // kendi SaveChanges'lerini yapsa da, doğrudan bağlam üzerinden
            // yapılan değişiklikler burada güvenceye alınır.
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();

            // Geri alınan değişikliklerin bağlamda "kaydedilmiş" gibi
            // durmasını engelle: aksi hâlde aynı istekte yapılan sonraki bir
            // SaveChanges, geri alınmış veriyi tekrar yazmaya çalışır.
            _context.ChangeTracker.Clear();
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public async ValueTask DisposeAsync() => await DisposeTransactionAsync();

    private async ValueTask DisposeTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
