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
            .NotEmpty().WithMessage("E-posta adresi zorunludur.")
            .MaximumLength(256).WithMessage("E-posta adresi en fazla 256 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .Must(email => !email.Any(char.IsWhiteSpace))
            .WithMessage("E-posta adresi boşluk içeremez.");

    internal static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Parola zorunludur.")
            .MinimumLength(MinLength).WithMessage($"Parola en az {MinLength} karakter olmalıdır.")
            .MaximumLength(MaxLength).WithMessage($"Parola en fazla {MaxLength} karakter olabilir.")
            .Matches("[a-zçğıöşü]").WithMessage("Parola en az bir küçük harf içermelidir.")
            .Matches("[A-ZÇĞIİÖŞÜ]").WithMessage("Parola en az bir büyük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Parola en az bir rakam içermelidir.");
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).Email();

        RuleFor(x => x.Password).Password();

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad zorunludur.")
            .MaximumLength(64).WithMessage("Ad en fazla 64 karakter olabilir.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad zorunludur.")
            .MaximumLength(64).WithMessage("Soyad en fazla 64 karakter olabilir.");

        RuleFor(x => x.Nickname)
            .NotEmpty().WithMessage("Takma ad zorunludur.")
            .MinimumLength(3).WithMessage("Takma ad en az 3 karakter olmalıdır.")
            .MaximumLength(32).WithMessage("Takma ad en fazla 32 karakter olabilir.")
            // Takma ad sıralama tablosunda ve skor ekranında gösteriliyor.
            // Karakter kümesini kısıtlamak, görünen metinle oynayarak yapılan
            // taklit (ör. görsel olarak aynı Unicode karakterler) ve
            // enjeksiyon denemelerini baştan keser.
            .Matches("^[A-Za-z0-9ÇĞİÖŞÜçğıöşü_.-]+$")
            .WithMessage("Takma ad yalnızca harf, rakam, alt çizgi, nokta ve tire içerebilir.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi zorunludur.")
            .MaximumLength(256);

        // Girişte parola KURALLARI uygulanmaz, yalnızca boş olmadığı kontrol
        // edilir. Aksi hâlde eski (kurallardan önce oluşturulmuş) parolalarla
        // giriş yapılamaz hâle gelirdi; ayrıca hata mesajı üzerinden parola
        // politikası hakkında bilgi sızardı.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola zorunludur.")
            .MaximumLength(PasswordRules.MaxLength);
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Yenileme jetonu zorunludur.")
            .MaximumLength(512);
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mevcut parola zorunludur.");

        RuleFor(x => x.NewPassword).Password();

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("Yeni parola mevcut parolanızla aynı olamaz.");
    }
}
