namespace QuizArena.Core.Utilities.Security;

/// <summary>
/// "İsteği yapan kim?" sorusunun cevabını iş katmanına taşır.
/// </summary>
/// <remarks>
/// İş katmanı <c>HttpContext</c>'i tanımaz — tanısaydı BLL doğrudan ASP.NET
/// Core'a bağımlı hâle gelir, arka plan işinde/konsol uygulamasında yeniden
/// kullanılamaz ve birim testinde sahte bir HTTP bağlamı kurmak gerekirdi.
/// Bu arayüz sayesinde testte tek satırlık bir sahte (fake) yeterlidir.
/// </remarks>
public interface ICurrentUserService
{
    /// <summary>Kimlik doğrulanmışsa kullanıcı kimliği, aksi hâlde <c>null</c>.</summary>
    Guid? UserId { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Roles { get; }

    string? IpAddress { get; }

    bool IsInRole(string role);

    /// <summary>
    /// Kimlik doğrulanmış kullanıcı kimliğini döner; yoksa
    /// <see cref="Exceptions.UnauthorizedException"/> atar. Servislerde
    /// <c>UserId!.Value</c> yazma alışkanlığını (ve onun getirdiği
    /// <c>NullReferenceException</c> riskini) ortadan kaldırır.
    /// </summary>
    Guid RequireUserId();
}
