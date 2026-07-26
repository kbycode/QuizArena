using System.Globalization;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using QuizArena.Api.Configuration;
using QuizArena.Api.Extensions;
using QuizArena.Api.Hubs;
using QuizArena.Api.Middleware;
using QuizArena.BLL.DependencyResolvers.Autofac;
using QuizArena.BLL.Extensions;
using QuizArena.BLL.Notifications;
using QuizArena.Core.Extensions;
using QuizArena.DAL.Extensions;
using QuizArena.DAL.Seed;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

// ---------------------------------------------------------------------------
//  QuizArena API — uygulama girişi
//
//  Bu dosya bilinçli olarak kısa tutuldu: her sorumluluk kendi extension
//  metoduna taşındı (Extensions klasörü). Program.cs yalnızca "hangi parçalar,
//  hangi sırayla" sorusunu cevaplıyor.
//
//  Kurulumları iç içe yazmak iki maliyet doğurur: dosya okunamaz hâle gelir
//  ve aynı politikanın iki kez tanımlanması gibi hatalar gözden kaçar (CORS'ta
//  isimsiz bir politika, isimli olanı sessizce gölgeler).
// ---------------------------------------------------------------------------

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
//  Loglama (Serilog) — ayarlar appsettings'ten okunur
// ---------------------------------------------------------------------------
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    // InvariantCulture: log satırlarındaki tarih/sayı biçimi sunucunun
    // kültürüne göre değişmesin. Aksi hâlde aynı uygulamanın iki sunucusu
    // farklı biçimde log yazar ve log ayrıştırma (parsing) araçları bozulur.
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

// ---------------------------------------------------------------------------
//  Bağımlılık enjeksiyonu
//
//  Autofac yalnızca iş servisleri için devrede: [ValidationAspect],
//  [CacheAspect], [TransactionAspect] gibi nitelikler metot kesme
//  (interception) gerektiriyor; yerleşik konteyner bunu desteklemiyor.
// ---------------------------------------------------------------------------
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
    container.RegisterModule(new AutofacBusinessModule()));

// --- Katman kayıtları -------------------------------------------------------
builder.Services.AddCoreServices(builder.Configuration);
builder.Services.AddDataAccess(builder.Configuration);
builder.Services.AddBusinessServices();

// Geliştirmede admin parolası verilmemişse rastgele üretilip bir kez loga
// yazılmasına izin verilir. Üretimde bu bayrak kapalı kalır; parola yoksa
// yönetici hesabı hiç oluşturulmaz (bkz. DatabaseSeeder).
builder.Services.PostConfigure<SeedOptions>(options =>
    options.AllowGeneratedAdminPassword = builder.Environment.IsDevelopment());

// --- Gerçek zamanlı bildirim ------------------------------------------------
builder.Services.AddSignalR();

// AddBusinessServices, TryAdd ile NullGameNotifier'ı kaydediyor (iş katmanının
// bildirim altyapısı olmadan da çalışabilmesi için). Burada gerçek SignalR
// uygulamasını ekliyoruz; son kayıt kazandığı için bu geçerli olur.
builder.Services.AddSingleton<IGameNotifier, SignalRGameNotifier>();

// --- Web katmanı ------------------------------------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Enum'lar sayı değil isim olarak taşınır: yanıtta "difficulty": "Hard"
        // görmek, "difficulty": 3 görmekten hem okunaklı hem de kırılgan
        // olmayan bir sözleşmedir (enum değerlerinin sırası değişse bile
        // istemci bozulmaz). Okuma tarafında sayılar da kabul edilmeye
        // devam eder, yani mevcut istemciler kırılmaz.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddApiDocumentation();
builder.Services.AddJwtAuthentication();
builder.Services.AddConfiguredCors(builder.Configuration);
builder.Services.AddConfiguredRateLimiting(builder.Configuration);
builder.Services.AddConfiguredHealthChecks();

WebApplication app = builder.Build();

// ---------------------------------------------------------------------------
//  Veritabanı: şema + başlangıç verisi
// ---------------------------------------------------------------------------
await app.InitializeDatabaseAsync();

// ---------------------------------------------------------------------------
//  HTTP boru hattı — SIRA KRİTİK
// ---------------------------------------------------------------------------

// 1) İstek logu EN DIŞTA.
//
//    Sıra burada kritik ve sezgiye aykırı: Serilog'un istek logu, hata
//    yöneticisinin DIŞINDA olmak zorunda. Aksi hâlde (log içeride kalırsa)
//    istisna önce Serilog'dan geçer; o anda yanıt durum kodu henüz
//    belirlenmemiştir ve Serilog bunu "500" olarak yazar. İstemci 404/409
//    alsa bile loglar 500 dolu görünür — hata panoları uydurma sunucu
//    hatalarıyla kirlenir ve gerçek arızalar gürültünün içinde kaybolur.
//
//    Dışta olduğunda: istisnayı hata yöneticisi çözer, durum kodu kesinleşir,
//    Serilog gerçek kodu loglar. Ayrıca aynı hata iki kez loglanmaz.
app.UseSerilogRequestLogging();

// 2) Hata yönetimi: sonrasındaki her şeyin istisnasını yakalar.
app.UseAppExceptionHandling();

// 3) Güvenlik başlıkları: hata yanıtlarında da bulunmaları gerekir.
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Swagger yalnızca geliştirmede. Üretimde açık bırakmak, tüm uç listesini
    // ve şemayı saldırgana hazır hâlde sunmak olurdu.
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "QuizArena API v1");
        options.DocumentTitle = "QuizArena API";
        options.RoutePrefix = "swagger";
    });
}
else
{
    // HSTS yalnızca üretimde: tarayıcıya "bu alan adına hep HTTPS ile gel"
    // demek, yerel geliştirme sertifikalarıyla çalışmayı bozar.
    app.UseHsts();
    app.UseHttpsRedirection();
}

// 4) Statik dosyalar: wwwroot altındaki oynanabilir demo arayüzü.
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        // Cache-Control: no-cache
        //
        // Adına rağmen "önbellekleme" DEĞİL, "kullanmadan önce doğrula"
        // demektir. Dosya diskte tutulur ama tarayıcı her seferinde ETag ile
        // sunucuya sorar; değişmemişse 304 (gövdesiz, ~100 bayt) döner.
        //
        // Neden gerekli: bu başlık olmadan tarayıcı SEZGİSEL önbellekleme
        // yapar (genelde dosyanın yaşının %10'u kadar) ve sunucuya hiç
        // sormadan eski kopyayı kullanır. Sonuç: yeni sürüm yayınlanır ama
        // kullanıcı günlerce eski JavaScript ile çalışmaya devam eder —
        // üstelik yeni API ile eski istemcinin uyuşmaması, teşhisi çok zor
        // hatalar üretir.
        //
        // Not: dosya adına sürüm damgası basan (app.a1b2c3.js) bir derleme
        // adımı olsaydı, o dosyalara 1 yıllık 'immutable' vermek daha verimli
        // olurdu. Bu projede derleme adımı bilinçli olarak yok, bu yüzden
        // doğrulamalı önbellek doğru denge.
        context.Context.Response.Headers.CacheControl = "no-cache";
    }
});

// 5) CORS, kimlik doğrulamadan önce: ön kontrol (preflight) istekleri kimlik
//    doğrulama gerektirmez ve gerektirirse tarayıcı isteği hiç göndermez.
app.UseCors(CorsOptions.PolicyName);

// 6) Hız sınırı, kimlik doğrulamadan SONRA: sınır kullanıcı bazlı
//    bölümlendiği için kimliğin çözülmüş olması gerekir.
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHub<QuizHub>("/hubs/quiz");

// ---------------------------------------------------------------------------
//  Sağlık kontrolleri
//
//  /health/live  : "süreç ayakta mı?" — bağımlılıklara bakmaz.
//  /health/ready : "istek alabilir miyim?" — veritabanına da bakar.
//
//  Ayrım önemli: veritabanı geçici olarak erişilemez olduğunda yük dengeleyici
//  süreci yeniden BAŞLATMAMALI (liveness), sadece trafiği KESMELİ (readiness).
//  Tek bir /health ucu bu ikisini karıştırır ve gereksiz yeniden başlatma
//  döngülerine yol açar.
// ---------------------------------------------------------------------------
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

await app.RunAsync();
