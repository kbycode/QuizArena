using QuizArena.Entities.Dtos.Users;
using FluentValidation;

namespace QuizArena.BLL.Validation;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(_ => ValidationMessages.FirstNameRequired)
            .MaximumLength(64);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(_ => ValidationMessages.LastNameRequired)
            .MaximumLength(64);

        RuleFor(x => x.Nickname)
            .NotEmpty().WithMessage(_ => ValidationMessages.NicknameRequired)
            .MinimumLength(3).WithMessage(_ => ValidationMessages.NicknameMinLength)
            .MaximumLength(32)
            .Matches("^[A-Za-z0-9ÇĞİÖŞÜçğıöşü_.-]+$")
            .WithMessage(_ => ValidationMessages.NicknameCharset);

        RuleFor(x => x.City)
            .MaximumLength(64)
            .When(x => x.City is not null);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(512)
            // Avatar adresi bir <img src> içinde kullanılacak. Şema kontrolü
            // olmadan "javascript:" veya "data:text/html" gibi bir değer
            // saklanabilir; bu, profili görüntüleyen herkeste betik çalıştırma
            // (saklı XSS) anlamına gelir.
            .Must(BeHttpUrl).WithMessage(_ => ValidationMessages.AvatarUrlScheme)
            .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));

        RuleFor(x => x.BirthDate)
            .Must(date => date!.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage(_ => ValidationMessages.BirthDateNotFuture)
            .When(x => x.BirthDate is not null);
    }

    private static bool BeHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
