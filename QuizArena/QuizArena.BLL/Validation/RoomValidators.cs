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
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Kategori seçilmelidir.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Etkinlik adı zorunludur.")
            .MinimumLength(3).WithMessage("Etkinlik adı en az 3 karakter olmalıdır.")
            .MaximumLength(64).WithMessage("Etkinlik adı en fazla 64 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(512).WithMessage("Açıklama en fazla 512 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.ScheduledStartUtc)
            .NotEmpty().WithMessage("Başlangıç zamanı zorunludur.");

        RuleFor(x => x.QuestionCount)
            .InclusiveBetween(GameRules.MinQuestionCount, GameRules.MaxQuestionCount)
            .WithMessage($"Soru sayısı {GameRules.MinQuestionCount}-{GameRules.MaxQuestionCount} arasında olmalıdır.");

        RuleFor(x => x.SecondsPerQuestion)
            .InclusiveBetween(GameRules.MinSecondsPerQuestion, GameRules.MaxSecondsPerQuestion)
            .WithMessage($"Soru süresi {GameRules.MinSecondsPerQuestion}-{GameRules.MaxSecondsPerQuestion} saniye arasında olmalıdır.");

        // Etkinlik her zaman çok oyunculudur: alt sınır 2.
        RuleFor(x => x.MaxPlayers)
            .InclusiveBetween(2, GameRules.MaxPlayers)
            .WithMessage($"Kontenjan 2-{GameRules.MaxPlayers} arasında olmalıdır.");
    }
}
