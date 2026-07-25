using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace QuizArena.Core.Aspects.Performance;

/// <summary>
/// Metot belirlenen süreyi aşarsa uyarı loglar.
/// </summary>
/// <remarks>
/// <para>
/// Kullanım: <c>[PerformanceAspect(ThresholdMilliseconds = 500)]</c>
/// </para>
/// <para>
/// Eşik altındaki çağrılar için <b>hiç log üretilmez</b>. Amaç, log hacmini
/// şişirmeden yavaşlayan noktaları görünür kılmak: üretimde "hangi sorgu
/// yavaşladı" sorusunun cevabı, ölçüm kodu iş metoduna karışmadan elde edilir.
/// </para>
/// </remarks>
public sealed class PerformanceAspect : AspectAttribute
{
    private const string StopwatchState = "__stopwatch";

    public PerformanceAspect()
    {
        // Dıştaki ölçüm gerçek toplam süreyi görmeli: en önce başlasın.
        Order = 5;
    }

    public int ThresholdMilliseconds { get; init; } = 500;

    public override void OnBefore(AspectContext context)
        => context.State[StopwatchState] = Stopwatch.StartNew();

    public override ValueTask OnFinallyAsync(AspectContext context)
    {
        if (!context.State.TryGetValue(StopwatchState, out object? state) || state is not Stopwatch stopwatch)
        {
            return ValueTask.CompletedTask;
        }

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds < ThresholdMilliseconds)
        {
            return ValueTask.CompletedTask;
        }

        context.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("QuizArena.Performance")
            .LogWarning(
                "Yavaş çalışma: {Type}.{Method} {Elapsed} ms sürdü (eşik {Threshold} ms).",
                context.Method.DeclaringType?.Name,
                context.Method.Name,
                stopwatch.ElapsedMilliseconds,
                ThresholdMilliseconds);

        return ValueTask.CompletedTask;
    }
}
