using QuizArena.Entities.Dtos.Auth;
using FluentValidation;

namespace QuizArena.BLL.Validation;

/// <summary>
/// Parola kuralları tek yerde.
/// </summary>
/// <remarks>
/// <para>
/// Politika tek yerde tanımlı; kayıt ve parola değiştirme aynı kuralı okur.
/// İki yerde ayrı tanımlansaydı biri güncellenip diğeri unutulduğunda
/// sistemde iki farklı parola gücü oluşurdu.
/// </para>
/// <para>
/// Kural seti NIST SP 800-63B önerisine yakın tutuldu: <b>uzunluk</b> en
/// önemli faktördür. Karakter çeşitliliği zorunluluğu ise ölçülü: aşırı
/// katı kurallar kullanıcıyı <c>Parola123!</c> gibi tahmin edilebilir
/// kalıplara veya parolayı bir kâğıda yazmaya iter.
/// </para>
/// </remarks>
internal static class PasswordRules
{
    internal const int MinLength = 8;
    internal const int MaxLength = 128;

    /// <summary>
    /// E-posta kuralları.
    /// </summary>
    /// <remarks>
    /// FluentValidation'ın <c>EmailAddress()</c> kuralı bilinçli olarak
    /// <b>gevşektir</b>: RFC 5322'ye göre teknik olarak geçerli olan tuhaf
    /// adresleri (ör. tırnaklı yerel bölümde boşluk) reddetmez. Buna güvenip
    /// bırakmak, <c>"ali soyad@ornek.test"</c> gibi pratikte teslim edilemeyen
    /// adreslerle kayıt açılmasına izin verir. Bu yüzden boşluk kontrolü
    /// ayrıca ekleniyor.
    /// </remarks>
    internal static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage(_ => ValidationMessages.EmailRequired)
            .MaximumLength(256).WithMessage(_ => ValidationMessages.EmailMaxLength)
            .EmailAddress().WithMessage(_ => ValidationMessages.EmailInvalid)
            .Must(email => !email.Any(char.IsWhiteSpace))
            .WithMessage(_ => ValidationMessages.EmailNoWhitespace);

    internal static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage(_ => ValidationMessages.PasswordRequired)
            .MinimumLength(MinLength).WithMessage(_ => ValidationMessages.PasswordMinLength(MinLength))
            .MaximumLength(MaxLength).WithMessage(_ => ValidationMessages.PasswordMaxLength(MaxLength))
            .Matches("[a-zçğıöşü]").WithMessage(_ => ValidationMessages.PasswordNeedsLower)
            .Matches("[A-ZÇĞIİÖŞÜ]").WithMessage(_ => ValidationMessages.PasswordNeedsUpper)
            .Matches("[0-9]").WithMessage(_ => ValidationMessages.PasswordNeedsDigit);
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).Email();

        RuleFor(x => x.Password).Password();

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(_ => ValidationMessages.FirstNameRequired)
            .MaximumLength(64).WithMessage(_ => ValidationMessages.FirstNameMaxLength);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(_ => ValidationMessages.LastNameRequired)
            .MaximumLength(64).WithMessage(_ => ValidationMessages.LastNameMaxLength);

        RuleFor(x => x.Nickname)
            .NotEmpty().WithMessage(_ => ValidationMessages.NicknameRequired)
            .MinimumLength(3).WithMessage(_ => ValidationMessages.NicknameMinLength)
            .MaximumLength(32).WithMessage(_ => ValidationMessages.NicknameMaxLength)
            // Takma ad sıralama tablosunda ve skor ekranında gösteriliyor.
            // Karakter kümesini kısıtlamak, görünen metinle oynayarak yapılan
            // taklit (ör. görsel olarak aynı Unicode karakterler) ve
            // enjeksiyon denemelerini baştan keser.
            .Matches("^[A-Za-z0-9ÇĞİÖŞÜçğıöşü_.-]+$")
            .WithMessage(_ => ValidationMessages.NicknameCharset);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => ValidationMessages.EmailRequired)
            .MaximumLength(256);

        // Girişte parola KURALLARI uygulanmaz, yalnızca boş olmadığı kontrol
        // edilir. Aksi hâlde eski (kurallardan önce oluşturulmuş) parolalarla
        // giriş yapılamaz hâle gelirdi; ayrıca hata mesajı üzerinden parola
        // politikası hakkında bilgi sızardı.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_ => ValidationMessages.PasswordRequired)
            .MaximumLength(PasswordRules.MaxLength);
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage(_ => ValidationMessages.RefreshTokenRequired)
            .MaximumLength(512);
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage(_ => ValidationMessages.CurrentPasswordRequired);

        RuleFor(x => x.NewPassword).Password();

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage(_ => ValidationMessages.NewPasswordMustDiffer);
    }
}
