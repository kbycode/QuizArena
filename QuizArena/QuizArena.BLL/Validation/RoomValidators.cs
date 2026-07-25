using QuizArena.BLL.Constants;
using QuizArena.Entities.Dtos.Play;
using QuizArena.Entities.Dtos.Rooms;
using QuizArena.Entities.Enums;
using FluentValidation;

namespace QuizArena.BLL.Validation;

public sealed class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Kategori seçilmelidir.");

        RuleFor(x => x.Name)
            .MaximumLength(64).WithMessage("Oda adı en fazla 64 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Mode).IsInEnum().WithMessage("Geçersiz oyun kipi.");

        // Sınırlar GameRules'tan okunuyor: doğrulama ile oyun motoru asla
        // farklı sınırlar uygulamaz.
        RuleFor(x => x.QuestionCount)
            .InclusiveBetween(GameRules.MinQuestionCount, GameRules.MaxQuestionCount)
            .WithMessage($"Soru sayısı {GameRules.MinQuestionCount}-{GameRules.MaxQuestionCount} arasında olmalıdır.");

        RuleFor(x => x.SecondsPerQuestion)
            .InclusiveBetween(GameRules.MinSecondsPerQuestion, GameRules.MaxSecondsPerQuestion)
            .WithMessage($"Soru süresi {GameRules.MinSecondsPerQuestion}-{GameRules.MaxSecondsPerQuestion} saniye arasında olmalıdır.");

        RuleFor(x => x.MaxPlayers)
            .InclusiveBetween(GameRules.MinPlayers, GameRules.MaxPlayers)
            .WithMessage($"Oyuncu sayısı {GameRules.MinPlayers}-{GameRules.MaxPlayers} arasında olmalıdır.");

        // Tek kişilik odada oyuncu sınırı 1 olmak zorunda; aksi hâlde arayüz
        // "başkalarını bekle" durumuna düşer ve oyun hiç başlamaz.
        RuleFor(x => x.MaxPlayers)
            .Equal(1)
            .When(x => x.Mode == RoomMode.Solo)
            .WithMessage("Tek kişilik odada oyuncu sayısı 1 olmalıdır.");

        RuleFor(x => x.MaxPlayers)
            .GreaterThanOrEqualTo(2)
            .When(x => x.Mode != RoomMode.Solo)
            .WithMessage("Çok oyunculu odada en az 2 oyuncu olmalıdır.");
    }
}

public sealed class JoinRoomRequestValidator : AbstractValidator<JoinRoomRequest>
{
    public JoinRoomRequestValidator()
    {
        RuleFor(x => x.JoinCode)
            .NotEmpty().WithMessage("Katılım kodu zorunludur.")
            .Length(GameRules.JoinCodeLength)
            .WithMessage($"Katılım kodu {GameRules.JoinCodeLength} karakter olmalıdır.");
    }
}

public sealed class SubmitAnswerRequestValidator : AbstractValidator<SubmitAnswerRequest>
{
    public SubmitAnswerRequestValidator()
    {
        RuleFor(x => x.CompetitionQuestionId)
            .NotEmpty().WithMessage("Soru bilgisi eksik.");

        // SelectedAnswerId null olabilir ("pas geçiyorum"); ancak Guid.Empty
        // gönderilmesi bir istemci hatasıdır ve sessizce "pas" sayılmamalıdır.
        RuleFor(x => x.SelectedAnswerId)
            .NotEqual(Guid.Empty).WithMessage("Geçersiz şık.")
            .When(x => x.SelectedAnswerId is not null);
    }
}
