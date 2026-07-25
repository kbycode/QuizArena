using QuizArena.Core.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace QuizArena.Core.Aspects.Transaction;

/// <summary>
/// Metodun tamamını tek bir veritabanı işlemine (transaction) sarar:
/// hepsi olur ya da hiçbiri olmaz.
/// </summary>
/// <remarks>
/// <para>
/// Kullanım: <c>[TransactionAspect]</c>
/// </para>
/// <para>
/// <b>İç içe çağrılara dayanıklıdır.</b> Zaten açık bir transaction varsa yeni
/// bir tane açmaz ve commit/rollback kararını dıştaki sahibine bırakır
/// (<see cref="OwnsTransactionState"/>). Bu kontrol olmadan, aspect uygulanmış
/// bir servis aspect uygulanmış başka bir servisi çağırdığında içteki metot
/// dıştakinin işlemini erken commit ederdi — atomiklik sessizce bozulurdu.
/// </para>
/// </remarks>
public sealed class TransactionAspect : AspectAttribute
{
    private const string OwnsTransactionState = "__ownsTransaction";

    public TransactionAspect()
    {
        // Doğrulama ve önbellekten sonra: geçersiz veri veya önbellek isabeti
        // durumunda hiç transaction açılmaz.
        Order = 30;
    }

    public override void OnBefore(AspectContext context)
    {
        var unitOfWork = context.Services.GetRequiredService<IUnitOfWork>();

        if (unitOfWork.HasActiveTransaction)
        {
            context.State[OwnsTransactionState] = false;
            return;
        }

        unitOfWork.BeginTransaction();
        context.State[OwnsTransactionState] = true;
    }

    public override async ValueTask OnSuccessAsync(AspectContext context)
    {
        if (!OwnsTransaction(context))
        {
            return;
        }

        await context.Services.GetRequiredService<IUnitOfWork>().CommitAsync();
    }

    public override async ValueTask OnExceptionAsync(AspectContext context, Exception exception)
    {
        if (!OwnsTransaction(context))
        {
            return;
        }

        await context.Services.GetRequiredService<IUnitOfWork>().RollbackAsync();
    }

    private static bool OwnsTransaction(AspectContext context)
        => context.State.TryGetValue(OwnsTransactionState, out object? owns) && owns is true;
}
