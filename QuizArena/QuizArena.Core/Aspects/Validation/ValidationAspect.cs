using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace QuizArena.Core.Aspects.Validation;

/// <summary>
/// Metoda gelen argümanı, belirtilen FluentValidation doğrulayıcısıyla
/// <b>iş kuralı çalışmadan önce</b> doğrular.
/// </summary>
/// <remarks>
/// <para>
/// Kullanım: <c>[ValidationAspect(typeof(CreateRoomRequestValidator))]</c>
/// </para>
/// <para>
/// <b>Neden sadece controller'daki <c>ModelState</c> yetmiyor?</b> Çünkü iş
/// katmanı yalnızca HTTP üzerinden çağrılmayabilir: arka plan görevi, SignalR
/// hub'ı, konsol aracı ya da başka bir servis de çağırabilir. Doğrulama, iş
/// kuralının hemen yanında durursa hangi kapıdan girilirse girilsin geçerlidir.
/// </para>
/// <para>
/// Doğrulama başarısızsa <see cref="FluentValidation.ValidationException"/>
/// atılır ve <c>ExceptionHandlingMiddleware</c> bunu alan bazlı hataları içeren
/// bir HTTP 400 <c>ValidationProblemDetails</c> yanıtına çevirir.
/// </para>
/// </remarks>
public sealed class ValidationAspect : AspectAttribute
{
    private readonly Type _validatorType;
    private readonly Type _validatedType;

    public ValidationAspect(Type validatorType)
    {
        ArgumentNullException.ThrowIfNull(validatorType);

        if (!typeof(IValidator).IsAssignableFrom(validatorType))
        {
            throw new ArgumentException(
                $"{validatorType.Name} bir FluentValidation doğrulayıcısı değil.", nameof(validatorType));
        }

        _validatorType = validatorType;
        _validatedType = ResolveValidatedType(validatorType);

        // Doğrulama en önce çalışır: geçersiz veri için transaction açmak,
        // önbellek anahtarı üretmek veya loglamak boşa iştir.
        Order = 10;
    }

    public override void OnBefore(AspectContext context)
    {
        IValidator validator = ResolveValidator(context.Services);

        foreach (object? argument in context.Arguments)
        {
            if (argument is null || !_validatedType.IsInstanceOfType(argument))
            {
                continue;
            }

            // ValidationContext<object> kullanımı bilinçli: doğrulayıcı derleme
            // anında bilinmediği için jenerik tipi burada kapatamıyoruz.
            // FluentValidation'ın jenerik olmayan IValidator arayüzü bu senaryo
            // için tasarlanmıştır.
            ValidationResult result = validator.Validate(new ValidationContext<object>(argument));

            if (!result.IsValid)
            {
                throw new FluentValidation.ValidationException(result.Errors);
            }
        }
    }

    /// <summary>
    /// Doğrulayıcıyı önce DI'dan ister (başka servislere ihtiyacı olabilir),
    /// kayıtlı değilse parametresiz kurucuyla üretir.
    /// </summary>
    private IValidator ResolveValidator(IServiceProvider services)
        => services.GetService(_validatorType) as IValidator
           ?? (IValidator)ActivatorUtilities.CreateInstance(services, _validatorType);

    private static Type ResolveValidatedType(Type validatorType)
    {
        Type? closedInterface = Array.Find(
            validatorType.GetInterfaces(),
            i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));

        return closedInterface?.GetGenericArguments()[0]
               ?? throw new ArgumentException(
                   $"{validatorType.Name} tipinin IValidator<T> uygulaması bulunamadı.", nameof(validatorType));
    }
}
