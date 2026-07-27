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
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage(_ => ValidationMessages.CategoryRequired);

        RuleFor(x => x.Name)
            .MaximumLength(64).WithMessage(_ => ValidationMessages.RoomNameMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Mode).IsInEnum().WithMessage(_ => ValidationMessages.InvalidRoomMode);

        // Sınırlar GameRules'tan okunuyor: doğrulama ile oyun motoru asla
        // farklı sınırlar uygulamaz.
        RuleFor(x => x.QuestionCount)
            .InclusiveBetween(GameRules.MinQuestionCount, GameRules.MaxQuestionCount)
            .WithMessage(_ => ValidationMessages.QuestionCountRange(GameRules.MinQuestionCount, GameRules.MaxQuestionCount));

        RuleFor(x => x.SecondsPerQuestion)
            .InclusiveBetween(GameRules.MinSecondsPerQuestion, GameRules.MaxSecondsPerQuestion)
            .WithMessage(_ => ValidationMessages.SecondsPerQuestionRange(GameRules.MinSecondsPerQuestion, GameRules.MaxSecondsPerQuestion));

        RuleFor(x => x.MaxPlayers)
            .InclusiveBetween(GameRules.MinPlayers, GameRules.MaxPlayers)
            .WithMessage(_ => ValidationMessages.PlayerCountRange(GameRules.MinPlayers, GameRules.MaxPlayers));

        // Tek kişilik odada oyuncu sınırı 1 olmak zorunda; aksi hâlde arayüz
        // "başkalarını bekle" durumuna düşer ve oyun hiç başlamaz.
        RuleFor(x => x.MaxPlayers)
            .Equal(1)
            .When(x => x.Mode == RoomMode.Solo)
            .WithMessage(_ => ValidationMessages.SoloRoomSinglePlayer);

        RuleFor(x => x.MaxPlayers)
            .GreaterThanOrEqualTo(2)
            .When(x => x.Mode != RoomMode.Solo)
            .WithMessage(_ => ValidationMessages.MultiplayerMinPlayers);
    }
}

public sealed class JoinRoomRequestValidator : AbstractValidator<JoinRoomRequest>
{
    public JoinRoomRequestValidator()
    {
        RuleFor(x => x.JoinCode)
            .NotEmpty().WithMessage(_ => ValidationMessages.JoinCodeRequired)
            .Length(GameRules.JoinCodeLength)
            .WithMessage(_ => ValidationMessages.JoinCodeLength(GameRules.JoinCodeLength));
    }
}

public sealed class SubmitAnswerRequestValidator : AbstractValidator<SubmitAnswerRequest>
{
    public SubmitAnswerRequestValidator()
    {
        RuleFor(x => x.CompetitionQuestionId)
            .NotEmpty().WithMessage(_ => ValidationMessages.QuestionReferenceMissing);

        // SelectedAnswerId null olabilir ("pas geçiyorum"); ancak Guid.Empty
        // gönderilmesi bir istemci hatasıdır ve sessizce "pas" sayılmamalıdır.
        RuleFor(x => x.SelectedAnswerId)
            .NotEqual(Guid.Empty).WithMessage(_ => ValidationMessages.InvalidAnswer)
            .When(x => x.SelectedAnswerId is not null);
    }
}

/// <summary>
/// Etkinlik oluşturma/güncelleme doğrulaması.
/// </summary>
/// <remarks>
/// Başlangıç zamanının gelecekte olması <b>burada</b> kontrol edilmiyor:
/// doğrulayıcılar saf (pure) tutuluyor, saat okumak bir yan etkidir ve
/// doğrulayıcıyı test edilemez hâle getirir. O kural iş katmanında
/// <c>IClock</c> üzerinden uygulanıyor.
/// </remarks>
public sealed class SaveEventRequestValidator : AbstractValidator<SaveEventRequest>
{
    public SaveEventRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage(_ => ValidationMessages.CategoryRequired);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => ValidationMessages.EventNameRequired)
            .MinimumLength(3).WithMessage(_ => ValidationMessages.EventNameMinLength)
            .MaximumLength(64).WithMessage(_ => ValidationMessages.EventNameMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(512).WithMessage(_ => ValidationMessages.DescriptionMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.ScheduledStartUtc)
            .NotEmpty().WithMessage(_ => ValidationMessages.StartTimeRequired);

        RuleFor(x => x.QuestionCount)
            .InclusiveBetween(GameRules.MinQuestionCount, GameRules.MaxQuestionCount)
            .WithMessage(_ => ValidationMessages.QuestionCountRange(GameRules.MinQuestionCount, GameRules.MaxQuestionCount));

        RuleFor(x => x.SecondsPerQuestion)
            .InclusiveBetween(GameRules.MinSecondsPerQuestion, GameRules.MaxSecondsPerQuestion)
            .WithMessage(_ => ValidationMessages.SecondsPerQuestionRange(GameRules.MinSecondsPerQuestion, GameRules.MaxSecondsPerQuestion));

        // Etkinlik her zaman çok oyunculudur: alt sınır 2.
        RuleFor(x => x.MaxPlayers)
            .InclusiveBetween(2, GameRules.MaxPlayers)
            .WithMessage(_ => ValidationMessages.EventCapacityRange(2, GameRules.MaxPlayers));
    }
}
