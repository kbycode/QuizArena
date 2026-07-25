namespace QuizArena.Api.Middleware;

/// <summary>
/// Tarayıcı tarafı savunma başlıklarını ekler.
/// </summary>
/// <remarks>
/// Bu API bir demo arayüzü de sunduğu (wwwroot) için başlıklar hem JSON hem
/// HTML yanıtları için anlamlı. Her başlığın somut bir işi var:
/// <list type="bullet">
///   <item>
///     <b>X-Content-Type-Options: nosniff</b> — Tarayıcının içerik türünü
///     "tahmin etmesini" engeller. Aksi hâlde <c>text/plain</c> olarak dönen
///     kullanıcı içeriği HTML sayılıp betik çalıştırabilir.
///   </item>
///   <item>
///     <b>X-Frame-Options: DENY</b> — Sayfanın başka bir sitenin
///     <c>&lt;iframe&gt;</c>'ine gömülmesini engeller (clickjacking).
///   </item>
///   <item>
///     <b>Referrer-Policy</b> — Başka siteye geçişte tam URL'in (içinde
///     kimlik/kod olabilir) referrer başlığıyla sızmasını engeller.
///   </item>
///   <item>
///     <b>Content-Security-Policy</b> — Betik/stil kaynaklarını kısıtlar;
///     saklı XSS'in etkisini sınırlar.
///   </item>
/// </list>
/// </remarks>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        IHeaderDictionary headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        // Swagger arayüzü kendi betiklerini satır içi (inline) çalıştırdığı
        // için 'unsafe-inline' gerekiyor. Bu bilinçli bir ödünç: Swagger
        // yalnızca geliştirme ortamında açık (bkz. Program.cs).
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "connect-src 'self'; " +
            "font-src 'self' data:; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

        return _next(context);
    }
}
