using QuizArena.Core.Utilities.Results;
using Microsoft.AspNetCore.Mvc;

// ASP.NET Core'un minimal API'leri için de bir IResult tipi var
// (Microsoft.AspNetCore.Http.IResult) ve Web SDK'sı onu örtük using olarak
// ekliyor. İkisi karışmasın diye iş katmanının sonuç tipine takma ad veriyoruz.
using BusinessResult = QuizArena.Core.Utilities.Results.IResult;

namespace QuizArena.Api.Controllers;

/// <summary>
/// Tüm controller'ların ortak atası.
/// </summary>
/// <remarks>
/// <para>
/// Orijinal koddaki her controller metodu şu kalıbı tekrarlıyordu:
/// </para>
/// <code>
/// var result = _service.GetList();
/// if (result.Success) return Ok(result.Data);
/// return BadRequest(result.Message);
/// </code>
/// <para>
/// Bu kalıbın iki sorunu vardı: (1) 13 controller × 6 metot = 78 kez
/// tekrarlanıyordu, (2) her hatayı <c>400 Bad Request</c> yapıyordu —
/// "kayıt bulunamadı" için 404, "yetkin yok" için 403 dönmesi gerekirken.
/// Doğru durum kodu artık istisna tipine göre
/// <c>ExceptionHandlingMiddleware</c> tarafından seçiliyor; burada kalan
/// tek iş, başarılı sonucu paketlemek.
/// </para>
/// </remarks>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Veri taşıyan sonucu HTTP yanıtına çevirir.</summary>
    protected IActionResult FromResult<T>(IDataResult<T> result) =>
        result.Success
            ? Ok(new { data = result.Data, message = NullIfEmpty(result.Message) })
            // Buraya normalde düşülmez: başarısız durumlar istisna olarak
            // atılır ve middleware tarafından ProblemDetails'e çevrilir.
            // Yine de bir servis 'ErrorDataResult' dönerse sessiz kalmıyoruz.
            : BadRequest(new ProblemDetails
            {
                Title = "İşlem başarısız",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest
            });

    /// <summary>Veri taşımayan sonucu HTTP yanıtına çevirir.</summary>
    protected IActionResult FromResult(BusinessResult result) =>
        result.Success
            ? Ok(new { message = NullIfEmpty(result.Message) })
            : BadRequest(new ProblemDetails
            {
                Title = "İşlem başarısız",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest
            });

    /// <summary>Yeni kaynak oluşturulduğunda 201 döner.</summary>
    protected IActionResult CreatedFromResult<T>(IDataResult<T> result, string routeName, object routeValues) =>
        result.Success
            ? CreatedAtRoute(routeName, routeValues, new { data = result.Data, message = NullIfEmpty(result.Message) })
            : FromResult(result);

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
