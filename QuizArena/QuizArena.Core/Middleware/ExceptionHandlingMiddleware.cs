using System.Text.Json;
using QuizArena.Core.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ValidationException = FluentValidation.ValidationException;

namespace QuizArena.Core.Middleware;

/// <summary>
/// Tek merkezden hata yönetimi: her istisnayı RFC 7807
/// (<c>application/problem+json</c>) biçiminde bir yanıta çevirir.
/// </summary>
/// <remarks>
/// <para>
/// Merkezî hata yönetimi olmadan bir <c>SqlException</c> ya da
/// <c>NullReferenceException</c> istemciye <b>tam yığın izi (stack trace) ve
/// bağlantı dizesiyle</b> düşer. Bu, saldırgana sunucu sürümünü, şemayı ve
/// dosya yollarını veren ciddi bir bilgi sızıntısıdır; middleware onu
/// yapısal olarak engeller.
/// </para>
/// <para>
/// Buradaki sözleşme nettir: <b>beklenen</b> hatalar (<see cref="AppException"/>)
/// kullanıcıya gösterilebilir mesaj taşır; <b>beklenmeyen</b> her şey
/// istemciye "beklenmeyen hata + izleme kimliği" olarak döner, ayrıntısı
/// yalnızca sunucu loguna yazılır. İstemciye dönen <c>traceId</c> ile
/// kullanıcının bildirdiği hata log kaydıyla birebir eşleştirilebilir.
/// </para>
/// </remarks>
public sealed class ExceptionHandlingMiddleware
{
    private const int ClientClosedRequest = 499;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        // Yanıt gövdesi yazılmaya başladıysa başlıkları değiştiremeyiz;
        // sessizce ezmeye çalışmak bozuk JSON üretir.
        if (context.Response.HasStarted)
        {
            _logger.LogError(exception, "Yanıt başladıktan sonra istisna oluştu, gövde değiştirilemedi.");
            return;
        }

        ProblemDetails problem = Map(exception, context);

        if (exception is AppException or ValidationException)
        {
            // Yığın izi (stack trace) BİLİNÇLİ olarak loglanmıyor.
            //
            // "Bu e-posta zaten kayıtlı" gibi durumlar beklenen iş sonuçlarıdır;
            // bunlar için 25 satırlık yığın izi yazmak log hacmini şişirir ve
            // gerçek arızaları gürültünün içinde görünmez kılar. Mesaj, yol ve
            // durum kodu teşhis için yeterli.
            _logger.LogWarning(
                "İş kuralı/doğrulama hatası: {Path} → {Status} ({Message})",
                context.Request.Path,
                problem.Status,
                exception.Message);
        }
        else
        {
            _logger.LogError(
                exception,
                "Beklenmeyen hata: {Path} (traceId: {TraceId})",
                context.Request.Path,
                context.TraceIdentifier);
        }

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json; charset=utf-8";

        // DİKKAT: Çalışma zamanı tipi açıkça verilmek zorunda.
        //
        // System.Text.Json, varsayılan olarak BİLDİRİLEN tipe göre serileştirir.
        // Değişken 'ProblemDetails' olarak bildirildiği için, türetilmiş
        // 'ValidationProblemDetails' üzerinde tanımlı 'Errors' sözlüğü
        // sessizce yanıttan düşüyordu: istemci 400 alıyor ama hangi alanın
        // neden reddedildiğini öğrenemiyordu. GetType() ile gerçek tip
        // verildiğinde tüm alanlar serileştirilir.
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, problem.GetType(), JsonOptions));
    }

    private ProblemDetails Map(Exception exception, HttpContext context)
    {
        ProblemDetails problem = exception switch
        {
            // FluentValidation: alan bazlı hataları ValidationProblemDetails'e
            // çeviriyoruz; istemci hangi alanın neden reddedildiğini görür.
            ValidationException validation => new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Title = "Doğrulama hatası",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Gönderilen veri geçerli değil. Ayrıntılar 'errors' alanında."
            },

            BusinessException => Problem("İş kuralı hatası", exception.Message, StatusCodes.Status400BadRequest),
            NotFoundException => Problem("Bulunamadı", exception.Message, StatusCodes.Status404NotFound),
            ConflictException => Problem("Çakışma", exception.Message, StatusCodes.Status409Conflict),
            ForbiddenException => Problem("Yetkisiz işlem", exception.Message, StatusCodes.Status403Forbidden),
            UnauthorizedException => Problem("Kimlik doğrulanamadı", exception.Message, StatusCodes.Status401Unauthorized),

            // 499 "Client Closed Request": standart değil (nginx kaynaklı) ama
            // yaygın. İstemci bağlantıyı kopardığında bunu 500 saymak, hata
            // grafiklerini gerçek olmayan hatalarla kirletir.
            OperationCanceledException => Problem(
                "İstek iptal edildi",
                "İstek tamamlanmadan iptal edildi.",
                ClientClosedRequest),

            // Beklenmeyen: mesaj sızdırılmaz.
            _ => Problem(
                "Beklenmeyen hata",
                _environment.IsDevelopment()
                    ? exception.ToString()
                    : "Sunucuda beklenmeyen bir hata oluştu. Sorun kaydedildi.",
                StatusCodes.Status500InternalServerError)
        };

        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        return problem;
    }

    private static ProblemDetails Problem(string title, string detail, int status) => new()
    {
        Title = title,
        Detail = detail,
        Status = status
    };
}
