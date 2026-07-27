using QuizArena.Core.Localization;

namespace QuizArena.BLL.Constants;

/// <summary>
/// Kullanıcıya gösterilen metinler (Türkçe / İngilizce).
/// </summary>
/// <remarks>
/// <para>
/// Alanlar <c>const</c> değil <b>salt okunur özellik</b>: dil isteğe göre
/// değiştiği için değer derleme zamanında sabitlenemez. Yine de dışarıdan
/// yazılamaz, yani "bir kod mesajı kalıcı olarak bozar" riski yok.
/// </para>
/// <para>
/// Dil, <c>Accept-Language</c> başlığından çözülür (bkz.
/// <see cref="CurrentLanguage"/>). İki tablonun aynı anahtarları taşıdığı
/// uygulama açılışında doğrulanır; eksik çeviri sessizce Türkçe metin
/// döndürmez, hata verir.
/// </para>
/// </remarks>
public static class Messages
{
    // --- Kimlik doğrulama ---------------------------------------------------
    public static string UserRegistered => Text[nameof(UserRegistered)];
    public static string LoginSuccessful => Text[nameof(LoginSuccessful)];
    public static string LoggedOut => Text[nameof(LoggedOut)];
    public static string TokenRefreshed => Text[nameof(TokenRefreshed)];
    public static string PasswordChanged => Text[nameof(PasswordChanged)];

    /// <summary>
    /// <b>Bilinçli olarak belirsiz.</b> "Kullanıcı bulunamadı" ile "parola
    /// yanlış" mesajlarını ayırmak, saldırgana hangi e-postaların sistemde
    /// kayıtlı olduğunu tek tek doğrulama imkânı verir (kullanıcı numaralandırma
    /// / account enumeration). Tek ve aynı mesaj bunu engeller.
    /// </summary>
    public static string InvalidCredentials => Text[nameof(InvalidCredentials)];

    public static string AccountLocked => Text[nameof(AccountLocked)];
    public static string AccountInactive => Text[nameof(AccountInactive)];
    public static string EmailAlreadyRegistered => Text[nameof(EmailAlreadyRegistered)];
    public static string NicknameAlreadyTaken => Text[nameof(NicknameAlreadyTaken)];
    public static string InvalidRefreshToken => Text[nameof(InvalidRefreshToken)];
    public static string CurrentPasswordIncorrect => Text[nameof(CurrentPasswordIncorrect)];
    public static string NewPasswordMustDiffer => Text[nameof(NewPasswordMustDiffer)];

    // --- Kullanıcı -----------------------------------------------------------
    public static string UserNotFound => Text[nameof(UserNotFound)];
    public static string ProfileUpdated => Text[nameof(ProfileUpdated)];
    public static string UserActivated => Text[nameof(UserActivated)];
    public static string UserDeactivated => Text[nameof(UserDeactivated)];
    public static string UserUnlocked => Text[nameof(UserUnlocked)];
    public static string ClaimAssigned => Text[nameof(ClaimAssigned)];
    public static string ClaimRevoked => Text[nameof(ClaimRevoked)];

    // --- Kategori ------------------------------------------------------------
    public static string CategoryNotFound => Text[nameof(CategoryNotFound)];
    public static string CategoryCreated => Text[nameof(CategoryCreated)];
    public static string CategoryUpdated => Text[nameof(CategoryUpdated)];
    public static string CategoryDeleted => Text[nameof(CategoryDeleted)];
    public static string CategoryNameInUse => Text[nameof(CategoryNameInUse)];
    public static string CategoryInactive => Text[nameof(CategoryInactive)];

    // --- Soru ----------------------------------------------------------------
    public static string QuestionNotFound => Text[nameof(QuestionNotFound)];
    public static string QuestionCreated => Text[nameof(QuestionCreated)];
    public static string QuestionUpdated => Text[nameof(QuestionUpdated)];
    public static string QuestionDeleted => Text[nameof(QuestionDeleted)];
    public static string QuestionNeedsOneCorrectAnswer => Text[nameof(QuestionNeedsOneCorrectAnswer)];

    // --- Oda -----------------------------------------------------------------
    public static string RoomNotFound => Text[nameof(RoomNotFound)];
    public static string RoomCreated => Text[nameof(RoomCreated)];
    public static string RoomJoined => Text[nameof(RoomJoined)];
    public static string RoomLeft => Text[nameof(RoomLeft)];
    public static string RoomCancelled => Text[nameof(RoomCancelled)];
    public static string RoomFull => Text[nameof(RoomFull)];
    public static string RoomAlreadyStarted => Text[nameof(RoomAlreadyStarted)];
    public static string RoomNotWaiting => Text[nameof(RoomNotWaiting)];
    public static string OnlyHostCanStart => Text[nameof(OnlyHostCanStart)];
    public static string OnlyHostCanCancel => Text[nameof(OnlyHostCanCancel)];
    public static string AlreadyInRoom => Text[nameof(AlreadyInRoom)];
    public static string NotInRoom => Text[nameof(NotInRoom)];
    public static string HostCannotLeave => Text[nameof(HostCannotLeave)];
    public static string SoloRoomCannotBeJoined => Text[nameof(SoloRoomCannotBeJoined)];
    public static string AlreadyInAnotherRoom => Text[nameof(AlreadyInAnotherRoom)];

    // --- Zamanlanmış etkinlik ------------------------------------------------
    public static string EventNotFound => Text[nameof(EventNotFound)];
    public static string EventCreated => Text[nameof(EventCreated)];
    public static string EventUpdated => Text[nameof(EventUpdated)];
    public static string EventCancelled => Text[nameof(EventCancelled)];
    public static string EventRegistered => Text[nameof(EventRegistered)];
    public static string EventWithdrawn => Text[nameof(EventWithdrawn)];
    public static string EventFull => Text[nameof(EventFull)];
    public static string EventAlreadyRegistered => Text[nameof(EventAlreadyRegistered)];
    public static string EventNotRegistered => Text[nameof(EventNotRegistered)];
    public static string EventAlreadyStarted => Text[nameof(EventAlreadyStarted)];
    public static string EventStartMustBeFuture => Text[nameof(EventStartMustBeFuture)];
    public static string NotAScheduledEvent => Text[nameof(NotAScheduledEvent)];
    public static string EventCancelledNoParticipants => Text[nameof(EventCancelledNoParticipants)];

    // --- Yarışma / oyun ------------------------------------------------------
    public static string NotEnoughQuestions => Text[nameof(NotEnoughQuestions)];
    public static string GameStarted => Text[nameof(GameStarted)];
    public static string NoActiveCompetition => Text[nameof(NoActiveCompetition)];
    public static string CompetitionNotFound => Text[nameof(CompetitionNotFound)];
    public static string CompetitionAlreadyFinished => Text[nameof(CompetitionAlreadyFinished)];
    public static string QuestionAlreadyAnswered => Text[nameof(QuestionAlreadyAnswered)];
    public static string QuestionNotServedYet => Text[nameof(QuestionNotServedYet)];
    public static string AnswerDoesNotBelongToQuestion => Text[nameof(AnswerDoesNotBelongToQuestion)];
    public static string CompetitionAbandoned => Text[nameof(CompetitionAbandoned)];
    public static string AnswerAccepted => Text[nameof(AnswerAccepted)];
    public static string CompetitionCompleted => Text[nameof(CompetitionCompleted)];

    // --- Yetki ---------------------------------------------------------------
    public static string OperationClaimNotFound => Text[nameof(OperationClaimNotFound)];
    public static string OperationClaimCreated => Text[nameof(OperationClaimCreated)];
    public static string OperationClaimUpdated => Text[nameof(OperationClaimUpdated)];
    public static string OperationClaimDeleted => Text[nameof(OperationClaimDeleted)];
    public static string OperationClaimNameInUse => Text[nameof(OperationClaimNameInUse)];
    public static string ClaimAlreadyAssigned => Text[nameof(ClaimAlreadyAssigned)];

    /// <summary>Kategoride yetersiz soru: kaç tane bulunduğunu da söyler.</summary>
    public static string NotEnoughQuestionsIn(int available) =>
        CurrentLanguage.Value == AppLanguage.English
            ? $"{NotEnoughQuestions} (This category has {available} usable questions.)"
            : $"{NotEnoughQuestions} (Bu kategoride {available} uygun soru var.)";

    /// <summary>Kontenjan, kayıtlı oyuncu sayısının altına çekilemez.</summary>
    public static string QuotaBelowRegistered(int registered) =>
        CurrentLanguage.Value == AppLanguage.English
            ? $"Capacity cannot go below the number of registered players ({registered})."
            : $"Kontenjan, kayıtlı oyuncu sayısının ({registered}) altına düşürülemez.";

    private static readonly LocalizedText Text = new(
        new Dictionary<string, string>
        {
            [nameof(UserRegistered)] = "Kaydınız oluşturuldu. Hoş geldiniz!",
            [nameof(LoginSuccessful)] = "Giriş başarılı.",
            [nameof(LoggedOut)] = "Çıkış yapıldı.",
            [nameof(TokenRefreshed)] = "Oturum yenilendi.",
            [nameof(PasswordChanged)] = "Parolanız güncellendi. Diğer oturumlarınız kapatıldı.",
            [nameof(InvalidCredentials)] = "E-posta veya parola hatalı.",
            [nameof(AccountLocked)] =
                "Çok sayıda hatalı giriş nedeniyle hesabınız geçici olarak kilitlendi. Lütfen birkaç dakika sonra tekrar deneyin.",
            [nameof(AccountInactive)] = "Hesabınız devre dışı bırakılmış. Lütfen yöneticiyle iletişime geçin.",
            [nameof(EmailAlreadyRegistered)] = "Bu e-posta adresi ile daha önce kayıt oluşturulmuş.",
            [nameof(NicknameAlreadyTaken)] = "Bu takma ad başka bir kullanıcı tarafından kullanılıyor.",
            [nameof(InvalidRefreshToken)] = "Oturum bilgisi geçersiz. Lütfen yeniden giriş yapın.",
            [nameof(CurrentPasswordIncorrect)] = "Mevcut parolanız hatalı.",
            [nameof(NewPasswordMustDiffer)] = "Yeni parola mevcut parolanızla aynı olamaz.",

            [nameof(UserNotFound)] = "Kullanıcı bulunamadı.",
            [nameof(ProfileUpdated)] = "Profiliniz güncellendi.",
            [nameof(UserActivated)] = "Kullanıcı hesabı etkinleştirildi.",
            [nameof(UserDeactivated)] = "Kullanıcı hesabı devre dışı bırakıldı.",
            [nameof(UserUnlocked)] = "Hesap kilidi kaldırıldı.",
            [nameof(ClaimAssigned)] = "Yetki atandı.",
            [nameof(ClaimRevoked)] = "Yetki kaldırıldı.",

            [nameof(CategoryNotFound)] = "Kategori bulunamadı.",
            [nameof(CategoryCreated)] = "Kategori oluşturuldu.",
            [nameof(CategoryUpdated)] = "Kategori güncellendi.",
            [nameof(CategoryDeleted)] = "Kategori kaldırıldı.",
            [nameof(CategoryNameInUse)] = "Bu adla bir kategori zaten var.",
            [nameof(CategoryInactive)] = "Bu kategori şu anda kullanılamıyor.",

            [nameof(QuestionNotFound)] = "Soru bulunamadı.",
            [nameof(QuestionCreated)] = "Soru eklendi.",
            [nameof(QuestionUpdated)] = "Soru güncellendi.",
            [nameof(QuestionDeleted)] = "Soru kaldırıldı.",
            [nameof(QuestionNeedsOneCorrectAnswer)] = "Soruda tam olarak bir doğru şık bulunmalıdır.",

            [nameof(RoomNotFound)] = "Oda bulunamadı.",
            [nameof(RoomCreated)] = "Oda oluşturuldu.",
            [nameof(RoomJoined)] = "Odaya katıldınız.",
            [nameof(RoomLeft)] = "Odadan ayrıldınız.",
            [nameof(RoomCancelled)] = "Oda iptal edildi.",
            [nameof(RoomFull)] = "Oda kapasitesi dolu.",
            [nameof(RoomAlreadyStarted)] = "Yarışma başladığı için odaya katılamazsınız.",
            [nameof(RoomNotWaiting)] = "Bu işlem yalnızca yarışma başlamadan önce yapılabilir.",
            [nameof(OnlyHostCanStart)] = "Yarışmayı yalnızca odayı kuran kişi başlatabilir.",
            [nameof(OnlyHostCanCancel)] = "Odayı yalnızca kuran kişi iptal edebilir.",
            [nameof(AlreadyInRoom)] = "Zaten bu odadasınız.",
            [nameof(NotInRoom)] = "Bu odanın katılımcısı değilsiniz.",
            [nameof(HostCannotLeave)] = "Odayı kuran kişi ayrılamaz; odayı iptal edebilir.",
            [nameof(SoloRoomCannotBeJoined)] = "Tek kişilik odalara katılım yapılamaz.",
            [nameof(AlreadyInAnotherRoom)] =
                "Devam eden bir yarışmanız var. Yeni oda kurabilmek için önce onu tamamlayın veya iptal edin.",

            [nameof(EventNotFound)] = "Etkinlik bulunamadı.",
            [nameof(EventCreated)] = "Etkinlik oluşturuldu.",
            [nameof(EventUpdated)] = "Etkinlik güncellendi.",
            [nameof(EventCancelled)] = "Etkinlik iptal edildi.",
            [nameof(EventRegistered)] = "Etkinliğe kaydoldunuz.",
            [nameof(EventWithdrawn)] = "Etkinlik kaydınız iptal edildi.",
            [nameof(EventFull)] = "Etkinlik kontenjanı dolu.",
            [nameof(EventAlreadyRegistered)] = "Bu etkinliğe zaten kayıtlısınız.",
            [nameof(EventNotRegistered)] = "Bu etkinliğe kayıtlı değilsiniz.",
            [nameof(EventAlreadyStarted)] = "Etkinlik başladığı için bu işlem yapılamaz.",
            [nameof(EventStartMustBeFuture)] = "Etkinlik başlangıcı gelecekte bir zaman olmalıdır.",
            [nameof(NotAScheduledEvent)] = "Bu oda zamanlanmış bir etkinlik değil.",
            [nameof(EventCancelledNoParticipants)] = "Etkinlik, yeterli katılımcı olmadığı için iptal edildi.",

            [nameof(NotEnoughQuestions)] =
                "Bu kategoride yeterli sayıda soru yok. Daha az soruyla deneyin veya başka bir kategori seçin.",
            [nameof(GameStarted)] = "Yarışma başladı!",
            [nameof(NoActiveCompetition)] = "Devam eden bir yarışmanız yok.",
            [nameof(CompetitionNotFound)] = "Yarışma bulunamadı.",
            [nameof(CompetitionAlreadyFinished)] = "Bu yarışma tamamlanmış.",
            [nameof(QuestionAlreadyAnswered)] = "Bu soruyu zaten cevapladınız.",
            [nameof(QuestionNotServedYet)] = "Bu soru henüz size sunulmadı.",
            [nameof(AnswerDoesNotBelongToQuestion)] = "Seçilen şık bu soruya ait değil.",
            [nameof(CompetitionAbandoned)] = "Yarışma yarıda bırakıldı.",
            [nameof(AnswerAccepted)] = "Cevabınız kaydedildi.",
            [nameof(CompetitionCompleted)] = "Yarışma tamamlandı!",

            [nameof(OperationClaimNotFound)] = "Yetki tanımı bulunamadı.",
            [nameof(OperationClaimCreated)] = "Yetki tanımı oluşturuldu.",
            [nameof(OperationClaimUpdated)] = "Yetki tanımı güncellendi.",
            [nameof(OperationClaimDeleted)] = "Yetki tanımı kaldırıldı.",
            [nameof(OperationClaimNameInUse)] = "Bu adla bir yetki tanımı zaten var.",
            [nameof(ClaimAlreadyAssigned)] = "Bu yetki kullanıcıya zaten atanmış."
        },
        new Dictionary<string, string>
        {
            [nameof(UserRegistered)] = "Your account is ready. Welcome!",
            [nameof(LoginSuccessful)] = "Signed in.",
            [nameof(LoggedOut)] = "Signed out.",
            [nameof(TokenRefreshed)] = "Session refreshed.",
            [nameof(PasswordChanged)] = "Your password has been changed. Other sessions were signed out.",
            [nameof(InvalidCredentials)] = "Email or password is incorrect.",
            [nameof(AccountLocked)] =
                "Too many failed sign-in attempts, so your account is temporarily locked. Please try again in a few minutes.",
            [nameof(AccountInactive)] = "Your account has been deactivated. Please contact an administrator.",
            [nameof(EmailAlreadyRegistered)] = "An account with this email address already exists.",
            [nameof(NicknameAlreadyTaken)] = "That nickname is already taken.",
            [nameof(InvalidRefreshToken)] = "Your session is no longer valid. Please sign in again.",
            [nameof(CurrentPasswordIncorrect)] = "Your current password is incorrect.",
            [nameof(NewPasswordMustDiffer)] = "The new password must differ from your current one.",

            [nameof(UserNotFound)] = "User not found.",
            [nameof(ProfileUpdated)] = "Your profile has been updated.",
            [nameof(UserActivated)] = "Account activated.",
            [nameof(UserDeactivated)] = "Account deactivated.",
            [nameof(UserUnlocked)] = "Account unlocked.",
            [nameof(ClaimAssigned)] = "Permission granted.",
            [nameof(ClaimRevoked)] = "Permission revoked.",

            [nameof(CategoryNotFound)] = "Category not found.",
            [nameof(CategoryCreated)] = "Category created.",
            [nameof(CategoryUpdated)] = "Category updated.",
            [nameof(CategoryDeleted)] = "Category removed.",
            [nameof(CategoryNameInUse)] = "A category with this name already exists.",
            [nameof(CategoryInactive)] = "This category is currently unavailable.",

            [nameof(QuestionNotFound)] = "Question not found.",
            [nameof(QuestionCreated)] = "Question added.",
            [nameof(QuestionUpdated)] = "Question updated.",
            [nameof(QuestionDeleted)] = "Question removed.",
            [nameof(QuestionNeedsOneCorrectAnswer)] = "A question must have exactly one correct option.",

            [nameof(RoomNotFound)] = "Room not found.",
            [nameof(RoomCreated)] = "Room created.",
            [nameof(RoomJoined)] = "You joined the room.",
            [nameof(RoomLeft)] = "You left the room.",
            [nameof(RoomCancelled)] = "Room cancelled.",
            [nameof(RoomFull)] = "The room is full.",
            [nameof(RoomAlreadyStarted)] = "The game has already started, so you cannot join.",
            [nameof(RoomNotWaiting)] = "This is only possible before the game starts.",
            [nameof(OnlyHostCanStart)] = "Only the host can start the game.",
            [nameof(OnlyHostCanCancel)] = "Only the host can cancel the room.",
            [nameof(AlreadyInRoom)] = "You are already in this room.",
            [nameof(NotInRoom)] = "You are not a participant in this room.",
            [nameof(HostCannotLeave)] = "The host cannot leave; cancel the room instead.",
            [nameof(SoloRoomCannotBeJoined)] = "Solo rooms cannot be joined.",
            [nameof(AlreadyInAnotherRoom)] =
                "You already have a game in progress. Finish or cancel it before creating a new room.",

            [nameof(EventNotFound)] = "Event not found.",
            [nameof(EventCreated)] = "Event created.",
            [nameof(EventUpdated)] = "Event updated.",
            [nameof(EventCancelled)] = "Event cancelled.",
            [nameof(EventRegistered)] = "You are registered for the event.",
            [nameof(EventWithdrawn)] = "Your registration has been cancelled.",
            [nameof(EventFull)] = "The event is full.",
            [nameof(EventAlreadyRegistered)] = "You are already registered for this event.",
            [nameof(EventNotRegistered)] = "You are not registered for this event.",
            [nameof(EventAlreadyStarted)] = "The event has already started, so this is no longer possible.",
            [nameof(EventStartMustBeFuture)] = "The event must start at a future time.",
            [nameof(NotAScheduledEvent)] = "This room is not a scheduled event.",
            [nameof(EventCancelledNoParticipants)] = "The event was cancelled because nobody registered.",

            [nameof(NotEnoughQuestions)] =
                "This category does not have enough questions. Try fewer questions or another category.",
            [nameof(GameStarted)] = "The game has started!",
            [nameof(NoActiveCompetition)] = "You have no game in progress.",
            [nameof(CompetitionNotFound)] = "Competition not found.",
            [nameof(CompetitionAlreadyFinished)] = "This competition is already finished.",
            [nameof(QuestionAlreadyAnswered)] = "You have already answered this question.",
            [nameof(QuestionNotServedYet)] = "This question has not been served to you yet.",
            [nameof(AnswerDoesNotBelongToQuestion)] = "The selected option does not belong to this question.",
            [nameof(CompetitionAbandoned)] = "Competition abandoned.",
            [nameof(AnswerAccepted)] = "Your answer has been recorded.",
            [nameof(CompetitionCompleted)] = "Competition complete!",

            [nameof(OperationClaimNotFound)] = "Permission not found.",
            [nameof(OperationClaimCreated)] = "Permission created.",
            [nameof(OperationClaimUpdated)] = "Permission updated.",
            [nameof(OperationClaimDeleted)] = "Permission removed.",
            [nameof(OperationClaimNameInUse)] = "A permission with this name already exists.",
            [nameof(ClaimAlreadyAssigned)] = "The user already has this permission."
        });
}
