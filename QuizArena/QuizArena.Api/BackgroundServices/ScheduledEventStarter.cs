using QuizArena.BLL.Abstract;

namespace QuizArena.Api.BackgroundServices;

/// <summary>
/// Zamanı gelen etkinlikleri otomatik başlatan arka plan hizmeti.
/// </summary>
/// <remarks>
/// <para>
/// <b>Bu hizmet olmadan "zamanlanmış etkinlik" kavramı çalışmaz.</b>
/// Alternatifi, etkinliği ilk açan oyuncunun tarayıcısının başlatmasıydı;
/// o zaman etkinlik, birinin sayfayı açmasına kadar başlamaz — yani ilan
/// edilen saatte değil, rastgele bir anda başlardı.
/// </para>
/// <para>
/// <b>Kapsam (scope) yönetimi:</b> <see cref="BackgroundService"/> tekil
/// (singleton) olarak kaydedilir; iş servisleri ise istek ömürlüdür
/// (<c>Scoped</c>). Scoped bir servisi doğrudan enjekte etmek, uygulama
/// boyunca yaşayan tek bir <c>DbContext</c>'e yol açar — bellek sızıntısı ve
/// bayat izleme (stale tracking) demektir. Bu yüzden her turda yeni bir kapsam
/// açılıyor ve tur bitince kapatılıyor.
/// </para>
/// <para>
/// <b>Tek örnek varsayımı:</b> uygulama birden çok sunucuda çalıştırılırsa iki
/// örnek aynı etkinliği başlatmaya çalışabilir. Bugünkü tek örnekli dağıtımda
/// sorun değil; ölçeklenirken dağıtık kilit (ör. veritabanı satır kilidi veya
/// Redis) gerekir. Bu bilinçli bir sınır — ölçek gelmeden altyapı eklenmedi.
/// </para>
/// </remarks>
public sealed class ScheduledEventStarter : BackgroundService
{
    /// <summary>
    /// Kontrol aralığı.
    /// </summary>
    /// <remarks>
    /// 30 saniye, "ilan edilen saatte başladı" hissi için yeterince sık,
    /// boşa dönen sorgu üretmeyecek kadar da seyrek. Etkinlikler dakika
    /// hassasiyetinde planlandığı için daha sık kontrol bir şey kazandırmaz.
    /// </remarks>
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledEventStarter> _logger;

    public ScheduledEventStarter(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledEventStarter> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Etkinlik zamanlayıcı başladı ({IntervalSeconds} sn aralıkla kontrol edilecek).",
            CheckInterval.TotalSeconds);

        using var timer = new PeriodicTimer(CheckInterval);

        // İlk kontrolü hemen yap: uygulama kapalıyken saati gelen etkinlikler
        // açılıştan 30 saniye sonra değil, hemen başlasın.
        do
        {
            await ProcessOnceAsync(stoppingToken);
        }
        while (await SafeWaitAsync(timer, stoppingToken));

        _logger.LogInformation("Etkinlik zamanlayıcı durduruldu.");
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();

            IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            int started = await eventService.ProcessDueEventsAsync(cancellationToken);

            if (started > 0)
            {
                _logger.LogInformation("{Count} etkinlik başlatıldı.", started);
            }
        }
        catch (OperationCanceledException)
        {
            // Uygulama kapanıyor; hata değil.
        }
        catch (Exception exception)
        {
            // Buradaki bir istisna yakalanmazsa arka plan hizmeti tamamen ölür
            // ve etkinlikler bir daha hiç başlamaz — üstelik sessizce.
            _logger.LogError(exception, "Etkinlik zamanlayıcı turu başarısız oldu.");
        }
    }

    /// <summary>Kapanış sırasındaki iptali hata olarak yansıtmadan bekler.</summary>
    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
