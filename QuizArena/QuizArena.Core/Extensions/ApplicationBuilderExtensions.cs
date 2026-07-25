using QuizArena.Core.Middleware;
using Microsoft.AspNetCore.Builder;

namespace QuizArena.Core.Extensions;

/// <summary>Core katmanının sunduğu middleware'lerin kısayolları.</summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Global hata yönetimini devreye alır.
    /// <b>Boru hattının en başında</b> çağrılmalıdır: kendisinden sonra gelen
    /// her middleware'in istisnasını yakalayabilmesi için.
    /// </summary>
    public static IApplicationBuilder UseAppExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
