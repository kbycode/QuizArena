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
            .NotNull().WithMessage(_ => ValidationMessages.AnswersRequired)
            .Must(answers => answers.Count is >= MinOptions and <= MaxOptions)
            .WithMessage(_ => ValidationMessages.AnswerCountRange(MinOptions, MaxOptions))
            .Must(answers => answers.Count(a => a.IsCorrect) == 1)
            .WithMessage(_ => ValidationMessages.ExactlyOneCorrectAnswer)
            .Must(answers => answers.All(a => !string.IsNullOrWhiteSpace(a.Text)))
            .WithMessage(_ => ValidationMessages.AnswerTextRequired)
            .Must(answers => answers.All(a => a.Text.Length <= MaxAnswerTextLength))
            .WithMessage(_ => ValidationMessages.AnswerTextMaxLength(MaxAnswerTextLength))
            // Aynı metinli iki şık, oyuncu için çözümsüz bir soru üretir.
            .Must(answers => answers
                .Select(a => a.Text.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() == answers.Count)
            .WithMessage(_ => ValidationMessages.AnswersMustBeDistinct);
}

public sealed class CreateQuestionRequestValidator : AbstractValidator<CreateQuestionRequest>
{
    public CreateQuestionRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage(_ => ValidationMessages.CategoryRequired);

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage(_ => ValidationMessages.QuestionTextRequired)
            .MinimumLength(10).WithMessage(_ => ValidationMessages.QuestionTextMinLength)
            .MaximumLength(512);

        RuleFor(x => x.Difficulty).IsInEnum().WithMessage(_ => ValidationMessages.InvalidDifficulty);

        RuleFor(x => x.TimeLimitSeconds)
            .InclusiveBetween(5, 120).WithMessage(_ => ValidationMessages.TimeLimitRange);

        RuleFor(x => x.Explanation).MaximumLength(1_024).When(x => x.Explanation is not null);

        RuleFor(x => x.Answers).ValidAnswerSet();
    }
}

public sealed class UpdateQuestionRequestValidator : AbstractValidator<UpdateQuestionRequest>
{
    public UpdateQuestionRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage(_ => ValidationMessages.CategoryRequired);

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage(_ => ValidationMessages.QuestionTextRequired)
            .MinimumLength(10).WithMessage(_ => ValidationMessages.QuestionTextMinLength)
            .MaximumLength(512);

        RuleFor(x => x.Difficulty).IsInEnum().WithMessage(_ => ValidationMessages.InvalidDifficulty);

        RuleFor(x => x.TimeLimitSeconds)
            .InclusiveBetween(5, 120).WithMessage(_ => ValidationMessages.TimeLimitRange);

        RuleFor(x => x.Explanation).MaximumLength(1_024).When(x => x.Explanation is not null);

        RuleFor(x => x.Answers).ValidAnswerSet();
    }
}
