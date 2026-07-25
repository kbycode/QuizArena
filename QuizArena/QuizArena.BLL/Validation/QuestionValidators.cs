using QuizArena.Entities.Dtos.Questions;
using FluentValidation;

namespace QuizArena.BLL.Validation;

/// <summary>
/// Şık listesi kuralları.
/// </summary>
/// <remarks>
/// Buradaki en önemli kural <b>"tam olarak bir doğru şık"</b> koşuludur.
/// Bu kontrol olmadan:
/// <list type="bullet">
///   <item>Doğru şıkkı olmayan bir soru yarışmada çıkar ve hiç kimse bilemez.</item>
///   <item>İki doğru şıklı soru, seçilen şıkka göre farklı sonuç verir — oyun adil olmaz.</item>
/// </list>
/// <para>
/// Kurallar bir uzantı metodunda toplandı; <c>RuleFor(x =&gt; x.Answers)</c>
/// çağrısı her iki doğrulayıcıda kendi üye ifadesiyle yapılıyor (FluentValidation
/// alan adını üye ifadesinden çıkarır, bu yüzden ifade bir metoda sarılamaz).
/// </para>
/// </remarks>
internal static class AnswerListRules
{
    internal const int MinOptions = 2;
    internal const int MaxOptions = 6;
    internal const int MaxAnswerTextLength = 256;

    internal static IRuleBuilderOptions<T, IReadOnlyList<SaveAnswerRequest>> ValidAnswerSet<T>(
        this IRuleBuilder<T, IReadOnlyList<SaveAnswerRequest>> rule) =>
        rule
            .NotNull().WithMessage("Şıklar zorunludur.")
            .Must(answers => answers.Count is >= MinOptions and <= MaxOptions)
            .WithMessage($"Soruda en az {MinOptions}, en fazla {MaxOptions} şık olmalıdır.")
            .Must(answers => answers.Count(a => a.IsCorrect) == 1)
            .WithMessage("Soruda tam olarak bir doğru şık bulunmalıdır.")
            .Must(answers => answers.All(a => !string.IsNullOrWhiteSpace(a.Text)))
            .WithMessage("Şık metinleri boş olamaz.")
            .Must(answers => answers.All(a => a.Text.Length <= MaxAnswerTextLength))
            .WithMessage($"Şık metni en fazla {MaxAnswerTextLength} karakter olabilir.")
            // Aynı metinli iki şık, oyuncu için çözümsüz bir soru üretir.
            .Must(answers => answers
                .Select(a => a.Text.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() == answers.Count)
            .WithMessage("Şık metinleri birbirinden farklı olmalıdır.");
}

public sealed class CreateQuestionRequestValidator : AbstractValidator<CreateQuestionRequest>
{
    public CreateQuestionRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Kategori seçilmelidir.");

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Soru metni zorunludur.")
            .MinimumLength(10).WithMessage("Soru metni en az 10 karakter olmalıdır.")
            .MaximumLength(512);

        RuleFor(x => x.Difficulty).IsInEnum().WithMessage("Geçersiz zorluk değeri.");

        RuleFor(x => x.TimeLimitSeconds)
            .InclusiveBetween(5, 120).WithMessage("Süre limiti 5-120 saniye arasında olmalıdır.");

        RuleFor(x => x.Explanation).MaximumLength(1_024).When(x => x.Explanation is not null);

        RuleFor(x => x.Answers).ValidAnswerSet();
    }
}

public sealed class UpdateQuestionRequestValidator : AbstractValidator<UpdateQuestionRequest>
{
    public UpdateQuestionRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Kategori seçilmelidir.");

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Soru metni zorunludur.")
            .MinimumLength(10).WithMessage("Soru metni en az 10 karakter olmalıdır.")
            .MaximumLength(512);

        RuleFor(x => x.Difficulty).IsInEnum().WithMessage("Geçersiz zorluk değeri.");

        RuleFor(x => x.TimeLimitSeconds)
            .InclusiveBetween(5, 120).WithMessage("Süre limiti 5-120 saniye arasında olmalıdır.");

        RuleFor(x => x.Explanation).MaximumLength(1_024).When(x => x.Explanation is not null);

        RuleFor(x => x.Answers).ValidAnswerSet();
    }
}
