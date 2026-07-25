using System.Collections.Concurrent;
using System.Reflection;
using Castle.DynamicProxy;

namespace QuizArena.Core.Aspects;

/// <summary>
/// Aspect boru hattını çalıştıran tek kesici (interceptor).
/// Autofac, <c>EnableInterfaceInterceptors()</c> ile üretilen vekil (proxy)
/// nesnelerdeki her çağrıyı buraya yönlendirir.
/// </summary>
/// <remarks>
/// <para>
/// <b>Tasarım kararı:</b> Yaygın örneklerde her aspect'in kendisi bir
/// <c>IInterceptor</c> olur ve bağımlılıklarını statik bir servis bulucudan
/// (<c>ServiceTool.Resolve&lt;T&gt;()</c>) çeker. Burada tersi yapıldı: tek bir
/// kesici var, o da normal bağımlılık enjeksiyonuyla kuruluyor ve
/// <see cref="IServiceProvider"/>'ı aspect'lere <see cref="AspectContext"/>
/// içinde <b>geçiriyor</b>. Kazanım: global statik durum yok, kapsam (scope)
/// doğru, kesici birim testine açık.
/// </para>
/// <para>
/// <b>Asenkron doğruluğu:</b> Castle DynamicProxy'nin <c>Proceed()</c> çağrısı,
/// <c>Intercept</c> metodundan dönülmeden önce yapılmak zorundadır. Aşağıdaki
/// asenkron sarmalayıcılarda <c>Proceed()</c> ilk <c>await</c>'ten önce
/// çağrılır; C# asenkron metotlarının gövdesi ilk <c>await</c>'e kadar
/// senkron çalıştığı için bu koşul sağlanır. Naif uygulamalarda
/// <c>OnSuccess</c>, <c>Task</c> tamamlanmadan tetiklendiği için önbellek
/// <b>henüz üretilmemiş sonucu</b> saklar; bu sınıf sonucu <c>await</c>
/// ederek o hatayı yapmaz.
/// </para>
/// </remarks>
public sealed class AspectInterceptor : IInterceptor
{
    private static readonly ConcurrentDictionary<MethodInfo, AspectAttribute[]> AspectCache = new();

    private static readonly ConcurrentDictionary<Type, Func<IInvocation, AspectAttribute[], AspectContext, object>>
        AsyncHandlerCache = new();

    private static readonly MethodInfo InvokeTaskOfTMethod = typeof(AspectInterceptor)
        .GetMethod(nameof(InvokeTaskOfT), BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly IServiceProvider _services;

    public AspectInterceptor(IServiceProvider services) => _services = services;

    public void Intercept(IInvocation invocation)
    {
        AspectAttribute[] aspects = GetAspects(invocation);

        // Hiç aspect yoksa hiçbir maliyet doğurmadan geç.
        if (aspects.Length == 0)
        {
            invocation.Proceed();
            return;
        }

        Type declaredReturnType = invocation.Method.ReturnType;
        bool isTask = declaredReturnType == typeof(Task);
        bool isTaskOfT = declaredReturnType.IsGenericType
                         && declaredReturnType.GetGenericTypeDefinition() == typeof(Task<>);

        Type logicalReturnType = isTaskOfT
            ? declaredReturnType.GetGenericArguments()[0]
            : isTask ? typeof(void) : declaredReturnType;

        var context = new AspectContext(
            _services,
            invocation.MethodInvocationTarget ?? invocation.Method,
            invocation.Arguments,
            logicalReturnType);

        // OnBefore: sırayla. Biri kısa devre yaparsa kalanlar da çalışır
        // (ör. loglama, isabetli önbellek çağrısını da görmek ister).
        foreach (AspectAttribute aspect in aspects)
        {
            aspect.OnBefore(context);
        }

        if (isTaskOfT)
        {
            invocation.ReturnValue = AsyncHandlerCache
                .GetOrAdd(logicalReturnType, BuildAsyncHandler)(invocation, aspects, context);
        }
        else if (isTask)
        {
            invocation.ReturnValue = InvokeTask(invocation, aspects, context);
        }
        else
        {
            InvokeSync(invocation, aspects, context);
        }
    }

    // ---------------------------------------------------------------------
    //  Task<T> döndüren metotlar
    // ---------------------------------------------------------------------

    /// <summary>
    /// <see cref="HandleTaskOfTAsync{T}"/>'i <c>object</c> dönüşle sarar.
    /// Dönüş tipi bilinçli olarak <c>object</c>: yansımayla üretilen delege
    /// imzasının, <c>T</c>'den bağımsız tek bir <see cref="Func{T1,T2,T3,TResult}"/>
    /// olması gerekiyor — aksi hâlde her <c>T</c> için ayrı delege tipi
    /// üretmek zorunda kalırdık.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1859:Somut tip kullan",
        Justification = "Delege imzası T'den bağımsız olmak zorunda; bkz. AsyncHandlerCache.")]
    private static object InvokeTaskOfT<T>(IInvocation invocation, AspectAttribute[] aspects, AspectContext context)
        => HandleTaskOfTAsync<T>(invocation, aspects, context);

    private static async Task<T> HandleTaskOfTAsync<T>(
        IInvocation invocation,
        AspectAttribute[] aspects,
        AspectContext context)
    {
        try
        {
            if (!context.ShortCircuited)
            {
                // İlk await'ten önce: Castle'ın gereksinimi karşılanır.
                invocation.Proceed();
                var inner = (Task<T>)invocation.ReturnValue!;
                context.ReturnValue = await inner;
            }

            await RunSuccessAsync(aspects, context);
            return context.ReturnValue is T typed ? typed : default!;
        }
        catch (Exception exception)
        {
            await RunExceptionAsync(aspects, context, exception);
            throw;
        }
        finally
        {
            await RunFinallyAsync(aspects, context);
        }
    }

    // ---------------------------------------------------------------------
    //  Task döndüren metotlar
    // ---------------------------------------------------------------------

    private static async Task InvokeTask(IInvocation invocation, AspectAttribute[] aspects, AspectContext context)
    {
        try
        {
            if (!context.ShortCircuited)
            {
                invocation.Proceed();
                await (Task)invocation.ReturnValue!;
            }

            await RunSuccessAsync(aspects, context);
        }
        catch (Exception exception)
        {
            await RunExceptionAsync(aspects, context, exception);
            throw;
        }
        finally
        {
            await RunFinallyAsync(aspects, context);
        }
    }

    // ---------------------------------------------------------------------
    //  Senkron metotlar
    // ---------------------------------------------------------------------

    /// <summary>
    /// Senkron metotlar için yedek yol. Bu projedeki tüm servis metotları
    /// asenkron olduğu için pratikte kullanılmaz; kesici genel amaçlı
    /// kalabilmek için bu dalı da doğru şekilde ele alır.
    /// </summary>
    private static void InvokeSync(IInvocation invocation, AspectAttribute[] aspects, AspectContext context)
    {
        try
        {
            if (!context.ShortCircuited)
            {
                invocation.Proceed();
                context.ReturnValue = invocation.ReturnValue;
            }

            RunSuccessAsync(aspects, context).AsTask().GetAwaiter().GetResult();
            invocation.ReturnValue = context.ReturnValue;
        }
        catch (Exception exception)
        {
            RunExceptionAsync(aspects, context, exception).AsTask().GetAwaiter().GetResult();
            throw;
        }
        finally
        {
            RunFinallyAsync(aspects, context).AsTask().GetAwaiter().GetResult();
        }
    }

    // ---------------------------------------------------------------------
    //  Kanca çalıştırıcıları
    // ---------------------------------------------------------------------

    /// <summary>
    /// Sonrası kancaları <b>ters</b> sırada çalışır: en dışta açılan kaynak
    /// en son kapanır (transaction ↔ kilit sıralaması için gerekli).
    /// </summary>
    private static async ValueTask RunSuccessAsync(AspectAttribute[] aspects, AspectContext context)
    {
        for (int i = aspects.Length - 1; i >= 0; i--)
        {
            await aspects[i].OnSuccessAsync(context);
        }
    }

    private static async ValueTask RunExceptionAsync(AspectAttribute[] aspects, AspectContext context, Exception exception)
    {
        for (int i = aspects.Length - 1; i >= 0; i--)
        {
            // Bir aspect'in hata kancası patlarsa asıl istisnayı gizlememeli.
            try
            {
                await aspects[i].OnExceptionAsync(context, exception);
            }
            catch
            {
                // Yutuluyor: asıl istisna çağırana olduğu gibi ulaşmalı.
            }
        }
    }

    private static async ValueTask RunFinallyAsync(AspectAttribute[] aspects, AspectContext context)
    {
        for (int i = aspects.Length - 1; i >= 0; i--)
        {
            try
            {
                await aspects[i].OnFinallyAsync(context);
            }
            catch
            {
                // Aynı gerekçe.
            }
        }
    }

    // ---------------------------------------------------------------------
    //  Yansıma (reflection) önbellekleri
    // ---------------------------------------------------------------------

    /// <summary>
    /// Metoda uygulanan aspect'leri bulur. Yansıma pahalı olduğu için
    /// metot başına bir kez hesaplanıp önbelleklenir.
    /// Nitelikler hem arayüzde hem somut sınıfta, hem metotta hem sınıf
    /// düzeyinde tanımlanabilir.
    /// </summary>
    private static AspectAttribute[] GetAspects(IInvocation invocation)
    {
        return AspectCache.GetOrAdd(invocation.Method, _ =>
        {
            MethodInfo target = invocation.MethodInvocationTarget ?? invocation.Method;

            var aspects = new List<AspectAttribute>();
            aspects.AddRange(target.DeclaringType?.GetCustomAttributes<AspectAttribute>(inherit: true) ?? []);
            aspects.AddRange(target.GetCustomAttributes<AspectAttribute>(inherit: true));

            // Somut sınıfta hiç tanım yoksa arayüz sözleşmesine bak.
            if (aspects.Count == 0 && !ReferenceEquals(target, invocation.Method))
            {
                aspects.AddRange(invocation.Method.GetCustomAttributes<AspectAttribute>(inherit: true));
            }

            return [.. aspects.OrderBy(a => a.Order)];
        });
    }

    private static Func<IInvocation, AspectAttribute[], AspectContext, object> BuildAsyncHandler(Type resultType)
        => InvokeTaskOfTMethod
            .MakeGenericMethod(resultType)
            .CreateDelegate<Func<IInvocation, AspectAttribute[], AspectContext, object>>();
}
