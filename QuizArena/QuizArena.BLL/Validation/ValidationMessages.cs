using QuizArena.Core.Localization;

namespace QuizArena.BLL.Validation;

/// <summary>
/// Doğrulama hataları (Türkçe / İngilizce).
/// </summary>
/// <remarks>
/// <para>
/// Doğrulayıcılarda <c>.WithMessage(_ =&gt; ValidationMessages.X)</c> biçiminde,
/// yani <b>gecikmeli</b> kullanılır. Doğrudan <c>.WithMessage(string)</c>
/// verilseydi metin doğrulayıcı <i>kurulurken</i> hesaplanırdı; doğrulayıcı
/// tekil (singleton) olarak önbelleklenirse ilk isteğin dili tüm uygulamaya
/// yapışırdı. Lambda, metni her doğrulamada yeniden çözer ve yaşam süresinden
/// bağımsız olarak doğru dili verir.
/// </para>
/// <para>
/// Sayı içeren mesajlar sabit değil <b>metot</b>: sınırlar
/// <see cref="Constants.GameRules"/>'tan geliyor, iki dilde iki kez
/// yazılmaları gerekmiyor.
/// </para>
/// </remarks>
internal static class ValidationMessages
{
    private static bool IsEnglish => CurrentLanguage.Value == AppLanguage.English;

    // --- Sabit metinler ------------------------------------------------------
    internal static string FirstNameRequired => Text[nameof(FirstNameRequired)];
    internal static string FirstNameMaxLength => Text[nameof(FirstNameMaxLength)];
    internal static string LastNameRequired => Text[nameof(LastNameRequired)];
    internal static string LastNameMaxLength => Text[nameof(LastNameMaxLength)];
    internal static string NicknameRequired => Text[nameof(NicknameRequired)];
    internal static string NicknameMinLength => Text[nameof(NicknameMinLength)];
    internal static string NicknameMaxLength => Text[nameof(NicknameMaxLength)];
    internal static string NicknameCharset => Text[nameof(NicknameCharset)];
    internal static string EmailRequired => Text[nameof(EmailRequired)];
    internal static string EmailInvalid => Text[nameof(EmailInvalid)];
    internal static string EmailMaxLength => Text[nameof(EmailMaxLength)];
    internal static string EmailNoWhitespace => Text[nameof(EmailNoWhitespace)];
    internal static string PasswordRequired => Text[nameof(PasswordRequired)];
    internal static string PasswordNeedsUpper => Text[nameof(PasswordNeedsUpper)];
    internal static string PasswordNeedsLower => Text[nameof(PasswordNeedsLower)];
    internal static string PasswordNeedsDigit => Text[nameof(PasswordNeedsDigit)];
    internal static string CurrentPasswordRequired => Text[nameof(CurrentPasswordRequired)];
    internal static string NewPasswordMustDiffer => Text[nameof(NewPasswordMustDiffer)];
    internal static string RefreshTokenRequired => Text[nameof(RefreshTokenRequired)];
    internal static string AvatarUrlScheme => Text[nameof(AvatarUrlScheme)];
    internal static string BirthDateNotFuture => Text[nameof(BirthDateNotFuture)];
    internal static string CategoryRequired => Text[nameof(CategoryRequired)];
    internal static string CategoryNameRequired => Text[nameof(CategoryNameRequired)];
    internal static string CategoryNameMinLength => Text[nameof(CategoryNameMinLength)];
    internal static string DescriptionMaxLength => Text[nameof(DescriptionMaxLength)];
    internal static string IconMaxLength => Text[nameof(IconMaxLength)];
    internal static string ColorFormat => Text[nameof(ColorFormat)];
    internal static string QuestionTextRequired => Text[nameof(QuestionTextRequired)];
    internal static string QuestionTextMinLength => Text[nameof(QuestionTextMinLength)];
    internal static string QuestionReferenceMissing => Text[nameof(QuestionReferenceMissing)];
    internal static string InvalidDifficulty => Text[nameof(InvalidDifficulty)];
    internal static string TimeLimitRange => Text[nameof(TimeLimitRange)];
    internal static string AnswersRequired => Text[nameof(AnswersRequired)];
    internal static string AnswerTextRequired => Text[nameof(AnswerTextRequired)];
    internal static string AnswersMustBeDistinct => Text[nameof(AnswersMustBeDistinct)];
    internal static string ExactlyOneCorrectAnswer => Text[nameof(ExactlyOneCorrectAnswer)];
    internal static string InvalidAnswer => Text[nameof(InvalidAnswer)];
    internal static string RoomNameMaxLength => Text[nameof(RoomNameMaxLength)];
    internal static string InvalidRoomMode => Text[nameof(InvalidRoomMode)];
    internal static string SoloRoomSinglePlayer => Text[nameof(SoloRoomSinglePlayer)];
    internal static string MultiplayerMinPlayers => Text[nameof(MultiplayerMinPlayers)];
    internal static string JoinCodeRequired => Text[nameof(JoinCodeRequired)];
    internal static string EventNameRequired => Text[nameof(EventNameRequired)];
    internal static string EventNameMinLength => Text[nameof(EventNameMinLength)];
    internal static string EventNameMaxLength => Text[nameof(EventNameMaxLength)];
    internal static string StartTimeRequired => Text[nameof(StartTimeRequired)];
    internal static string UserRequired => Text[nameof(UserRequired)];
    internal static string ClaimRequired => Text[nameof(ClaimRequired)];
    internal static string ClaimNameRequired => Text[nameof(ClaimNameRequired)];
    internal static string ClaimNameCharset => Text[nameof(ClaimNameCharset)];

    // --- Sınır içeren metinler ----------------------------------------------
    internal static string PasswordMinLength(int min) => IsEnglish
        ? $"Password must be at least {min} characters."
        : $"Parola en az {min} karakter olmalıdır.";

    internal static string PasswordMaxLength(int max) => IsEnglish
        ? $"Password can be at most {max} characters."
        : $"Parola en fazla {max} karakter olabilir.";

    internal static string JoinCodeLength(int length) => IsEnglish
        ? $"The join code must be {length} characters."
        : $"Katılım kodu {length} karakter olmalıdır.";

    internal static string QuestionCountRange(int min, int max) => IsEnglish
        ? $"Question count must be between {min} and {max}."
        : $"Soru sayısı {min}-{max} arasında olmalıdır.";

    internal static string SecondsPerQuestionRange(int min, int max) => IsEnglish
        ? $"Time per question must be between {min} and {max} seconds."
        : $"Soru süresi {min}-{max} saniye arasında olmalıdır.";

    internal static string PlayerCountRange(int min, int max) => IsEnglish
        ? $"Player count must be between {min} and {max}."
        : $"Oyuncu sayısı {min}-{max} arasında olmalıdır.";

    internal static string EventCapacityRange(int min, int max) => IsEnglish
        ? $"Capacity must be between {min} and {max}."
        : $"Kontenjan {min}-{max} arasında olmalıdır.";

    internal static string AnswerCountRange(int min, int max) => IsEnglish
        ? $"A question must have between {min} and {max} options."
        : $"Soruda en az {min}, en fazla {max} şık olmalıdır.";

    internal static string AnswerTextMaxLength(int max) => IsEnglish
        ? $"Option text can be at most {max} characters."
        : $"Şık metni en fazla {max} karakter olabilir.";

    private static readonly LocalizedText Text = new(
        new Dictionary<string, string>
        {
            [nameof(FirstNameRequired)] = "Ad zorunludur.",
            [nameof(FirstNameMaxLength)] = "Ad en fazla 64 karakter olabilir.",
            [nameof(LastNameRequired)] = "Soyad zorunludur.",
            [nameof(LastNameMaxLength)] = "Soyad en fazla 64 karakter olabilir.",
            [nameof(NicknameRequired)] = "Takma ad zorunludur.",
            [nameof(NicknameMinLength)] = "Takma ad en az 3 karakter olmalıdır.",
            [nameof(NicknameMaxLength)] = "Takma ad en fazla 32 karakter olabilir.",
            [nameof(NicknameCharset)] = "Takma ad yalnızca harf, rakam, alt çizgi, nokta ve tire içerebilir.",
            [nameof(EmailRequired)] = "E-posta adresi zorunludur.",
            [nameof(EmailInvalid)] = "Geçerli bir e-posta adresi giriniz.",
            [nameof(EmailMaxLength)] = "E-posta adresi en fazla 256 karakter olabilir.",
            [nameof(EmailNoWhitespace)] = "E-posta adresi boşluk içeremez.",
            [nameof(PasswordRequired)] = "Parola zorunludur.",
            [nameof(PasswordNeedsUpper)] = "Parola en az bir büyük harf içermelidir.",
            [nameof(PasswordNeedsLower)] = "Parola en az bir küçük harf içermelidir.",
            [nameof(PasswordNeedsDigit)] = "Parola en az bir rakam içermelidir.",
            [nameof(CurrentPasswordRequired)] = "Mevcut parola zorunludur.",
            [nameof(NewPasswordMustDiffer)] = "Yeni parola mevcut parolanızla aynı olamaz.",
            [nameof(RefreshTokenRequired)] = "Yenileme jetonu zorunludur.",
            [nameof(AvatarUrlScheme)] = "Avatar adresi http veya https ile başlamalıdır.",
            [nameof(BirthDateNotFuture)] = "Doğum tarihi gelecekte olamaz.",
            [nameof(CategoryRequired)] = "Kategori seçilmelidir.",
            [nameof(CategoryNameRequired)] = "Kategori adı zorunludur.",
            [nameof(CategoryNameMinLength)] = "Kategori adı en az 2 karakter olmalıdır.",
            [nameof(DescriptionMaxLength)] = "Açıklama en fazla 512 karakter olabilir.",
            [nameof(IconMaxLength)] = "İkon en fazla 8 karakter olabilir.",
            [nameof(ColorFormat)] = "Renk #RRGGBB veya #RRGGBBAA biçiminde olmalıdır.",
            [nameof(QuestionTextRequired)] = "Soru metni zorunludur.",
            [nameof(QuestionTextMinLength)] = "Soru metni en az 10 karakter olmalıdır.",
            [nameof(QuestionReferenceMissing)] = "Soru bilgisi eksik.",
            [nameof(InvalidDifficulty)] = "Geçersiz zorluk değeri.",
            [nameof(TimeLimitRange)] = "Süre limiti 5-120 saniye arasında olmalıdır.",
            [nameof(AnswersRequired)] = "Şıklar zorunludur.",
            [nameof(AnswerTextRequired)] = "Şık metinleri boş olamaz.",
            [nameof(AnswersMustBeDistinct)] = "Şık metinleri birbirinden farklı olmalıdır.",
            [nameof(ExactlyOneCorrectAnswer)] = "Soruda tam olarak bir doğru şık bulunmalıdır.",
            [nameof(InvalidAnswer)] = "Geçersiz şık.",
            [nameof(RoomNameMaxLength)] = "Oda adı en fazla 64 karakter olabilir.",
            [nameof(InvalidRoomMode)] = "Geçersiz oyun kipi.",
            [nameof(SoloRoomSinglePlayer)] = "Tek kişilik odada oyuncu sayısı 1 olmalıdır.",
            [nameof(MultiplayerMinPlayers)] = "Çok oyunculu odada en az 2 oyuncu olmalıdır.",
            [nameof(JoinCodeRequired)] = "Katılım kodu zorunludur.",
            [nameof(EventNameRequired)] = "Etkinlik adı zorunludur.",
            [nameof(EventNameMinLength)] = "Etkinlik adı en az 3 karakter olmalıdır.",
            [nameof(EventNameMaxLength)] = "Etkinlik adı en fazla 64 karakter olabilir.",
            [nameof(StartTimeRequired)] = "Başlangıç zamanı zorunludur.",
            [nameof(UserRequired)] = "Kullanıcı seçilmelidir.",
            [nameof(ClaimRequired)] = "Yetki seçilmelidir.",
            [nameof(ClaimNameRequired)] = "Yetki adı zorunludur.",
            [nameof(ClaimNameCharset)] =
                "Yetki adı harf ile başlamalı; yalnızca harf, rakam, nokta, alt çizgi ve tire içerebilir."
        },
        new Dictionary<string, string>
        {
            [nameof(FirstNameRequired)] = "First name is required.",
            [nameof(FirstNameMaxLength)] = "First name can be at most 64 characters.",
            [nameof(LastNameRequired)] = "Last name is required.",
            [nameof(LastNameMaxLength)] = "Last name can be at most 64 characters.",
            [nameof(NicknameRequired)] = "Nickname is required.",
            [nameof(NicknameMinLength)] = "Nickname must be at least 3 characters.",
            [nameof(NicknameMaxLength)] = "Nickname can be at most 32 characters.",
            [nameof(NicknameCharset)] =
                "Nickname may contain only letters, digits, underscore, dot and hyphen.",
            [nameof(EmailRequired)] = "Email address is required.",
            [nameof(EmailInvalid)] = "Enter a valid email address.",
            [nameof(EmailMaxLength)] = "Email address can be at most 256 characters.",
            [nameof(EmailNoWhitespace)] = "Email address cannot contain spaces.",
            [nameof(PasswordRequired)] = "Password is required.",
            [nameof(PasswordNeedsUpper)] = "Password must contain at least one uppercase letter.",
            [nameof(PasswordNeedsLower)] = "Password must contain at least one lowercase letter.",
            [nameof(PasswordNeedsDigit)] = "Password must contain at least one digit.",
            [nameof(CurrentPasswordRequired)] = "Current password is required.",
            [nameof(NewPasswordMustDiffer)] = "The new password must differ from your current one.",
            [nameof(RefreshTokenRequired)] = "Refresh token is required.",
            [nameof(AvatarUrlScheme)] = "Avatar address must start with http or https.",
            [nameof(BirthDateNotFuture)] = "Date of birth cannot be in the future.",
            [nameof(CategoryRequired)] = "Select a category.",
            [nameof(CategoryNameRequired)] = "Category name is required.",
            [nameof(CategoryNameMinLength)] = "Category name must be at least 2 characters.",
            [nameof(DescriptionMaxLength)] = "Description can be at most 512 characters.",
            [nameof(IconMaxLength)] = "Icon can be at most 8 characters.",
            [nameof(ColorFormat)] = "Colour must be in #RRGGBB or #RRGGBBAA format.",
            [nameof(QuestionTextRequired)] = "Question text is required.",
            [nameof(QuestionTextMinLength)] = "Question text must be at least 10 characters.",
            [nameof(QuestionReferenceMissing)] = "The question reference is missing.",
            [nameof(InvalidDifficulty)] = "Invalid difficulty value.",
            [nameof(TimeLimitRange)] = "Time limit must be between 5 and 120 seconds.",
            [nameof(AnswersRequired)] = "Options are required.",
            [nameof(AnswerTextRequired)] = "Option text cannot be empty.",
            [nameof(AnswersMustBeDistinct)] = "Option texts must be different from each other.",
            [nameof(ExactlyOneCorrectAnswer)] = "A question must have exactly one correct option.",
            [nameof(InvalidAnswer)] = "Invalid option.",
            [nameof(RoomNameMaxLength)] = "Room name can be at most 64 characters.",
            [nameof(InvalidRoomMode)] = "Invalid game mode.",
            [nameof(SoloRoomSinglePlayer)] = "A solo room must have exactly 1 player.",
            [nameof(MultiplayerMinPlayers)] = "A multiplayer room needs at least 2 players.",
            [nameof(JoinCodeRequired)] = "Join code is required.",
            [nameof(EventNameRequired)] = "Event name is required.",
            [nameof(EventNameMinLength)] = "Event name must be at least 3 characters.",
            [nameof(EventNameMaxLength)] = "Event name can be at most 64 characters.",
            [nameof(StartTimeRequired)] = "Start time is required.",
            [nameof(UserRequired)] = "Select a user.",
            [nameof(ClaimRequired)] = "Select a permission.",
            [nameof(ClaimNameRequired)] = "Permission name is required.",
            [nameof(ClaimNameCharset)] =
                "Permission name must start with a letter and may contain only letters, digits, dot, underscore and hyphen."
        });
}
