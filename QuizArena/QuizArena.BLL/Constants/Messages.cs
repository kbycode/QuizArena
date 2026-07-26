namespace QuizArena.BLL.Constants;

/// <summary>
/// Kullanıcıya gösterilen metinler.
/// </summary>
/// <remarks>
/// <para>
/// İlk hâlde bu sınıftaki alanlar <c>public static string</c> idi; yani
/// <b>çalışma zamanında değiştirilebilirdi</b>. Herhangi bir kod
/// <c>Messages.UserNotFound = "..."</c> yazarak tüm uygulamanın mesajını
/// kalıcı olarak bozabilirdi. <c>const</c> bunu derleme zamanında imkânsız
/// kılar (ve derleyici değeri çağrı yerine gömdüğü için erişim de bedavadır).
/// </para>
/// <para>
/// Mesajlar ayrıca "Basariyla Eklendi" gibi ASCII-leştirilmiş hâlden düzgün
/// Türkçe'ye çevrildi; ileride birden çok dil gerekirse tek yapılacak iş
/// bu sınıfı bir kaynak (resx) dosyasına taşımaktır.
/// </para>
/// </remarks>
public static class Messages
{
    // --- Kimlik doğrulama ---------------------------------------------------
    public const string UserRegistered = "Kaydınız oluşturuldu. Hoş geldiniz!";
    public const string LoginSuccessful = "Giriş başarılı.";
    public const string LoggedOut = "Çıkış yapıldı.";
    public const string TokenRefreshed = "Oturum yenilendi.";
    public const string PasswordChanged = "Parolanız güncellendi. Diğer oturumlarınız kapatıldı.";

    /// <summary>
    /// <b>Bilinçli olarak belirsiz.</b> "Kullanıcı bulunamadı" ile "parola
    /// yanlış" mesajlarını ayırmak, saldırgana hangi e-postaların sistemde
    /// kayıtlı olduğunu tek tek doğrulama imkânı verir (kullanıcı numaralandırma
    /// / account enumeration). Tek ve aynı mesaj bunu engeller.
    /// </summary>
    public const string InvalidCredentials = "E-posta veya parola hatalı.";

    public const string AccountLocked =
        "Çok sayıda hatalı giriş nedeniyle hesabınız geçici olarak kilitlendi. Lütfen birkaç dakika sonra tekrar deneyin.";

    public const string AccountInactive = "Hesabınız devre dışı bırakılmış. Lütfen yöneticiyle iletişime geçin.";
    public const string EmailAlreadyRegistered = "Bu e-posta adresi ile daha önce kayıt oluşturulmuş.";
    public const string NicknameAlreadyTaken = "Bu takma ad başka bir kullanıcı tarafından kullanılıyor.";
    public const string InvalidRefreshToken = "Oturum bilgisi geçersiz. Lütfen yeniden giriş yapın.";
    public const string CurrentPasswordIncorrect = "Mevcut parolanız hatalı.";
    public const string NewPasswordMustDiffer = "Yeni parola mevcut parolanızla aynı olamaz.";

    // --- Kullanıcı -----------------------------------------------------------
    public const string UserNotFound = "Kullanıcı bulunamadı.";
    public const string ProfileUpdated = "Profiliniz güncellendi.";
    public const string UserActivated = "Kullanıcı hesabı etkinleştirildi.";
    public const string UserDeactivated = "Kullanıcı hesabı devre dışı bırakıldı.";
    public const string UserUnlocked = "Hesap kilidi kaldırıldı.";
    public const string ClaimAssigned = "Yetki atandı.";
    public const string ClaimRevoked = "Yetki kaldırıldı.";

    // --- Kategori ------------------------------------------------------------
    public const string CategoryNotFound = "Kategori bulunamadı.";
    public const string CategoryCreated = "Kategori oluşturuldu.";
    public const string CategoryUpdated = "Kategori güncellendi.";
    public const string CategoryDeleted = "Kategori kaldırıldı.";
    public const string CategoryNameInUse = "Bu adla bir kategori zaten var.";
    public const string CategoryInactive = "Bu kategori şu anda kullanılamıyor.";

    // --- Soru ----------------------------------------------------------------
    public const string QuestionNotFound = "Soru bulunamadı.";
    public const string QuestionCreated = "Soru eklendi.";
    public const string QuestionUpdated = "Soru güncellendi.";
    public const string QuestionDeleted = "Soru kaldırıldı.";
    public const string QuestionNeedsOneCorrectAnswer = "Soruda tam olarak bir doğru şık bulunmalıdır.";

    // --- Oda -----------------------------------------------------------------
    public const string RoomNotFound = "Oda bulunamadı.";
    public const string RoomCreated = "Oda oluşturuldu.";
    public const string RoomJoined = "Odaya katıldınız.";
    public const string RoomLeft = "Odadan ayrıldınız.";
    public const string RoomCancelled = "Oda iptal edildi.";
    public const string RoomFull = "Oda kapasitesi dolu.";
    public const string RoomAlreadyStarted = "Yarışma başladığı için odaya katılamazsınız.";
    public const string RoomNotWaiting = "Bu işlem yalnızca yarışma başlamadan önce yapılabilir.";
    public const string OnlyHostCanStart = "Yarışmayı yalnızca odayı kuran kişi başlatabilir.";
    public const string OnlyHostCanCancel = "Odayı yalnızca kuran kişi iptal edebilir.";
    public const string AlreadyInRoom = "Zaten bu odadasınız.";
    public const string NotInRoom = "Bu odanın katılımcısı değilsiniz.";
    public const string HostCannotLeave = "Odayı kuran kişi ayrılamaz; odayı iptal edebilir.";
    public const string SoloRoomCannotBeJoined = "Tek kişilik odalara katılım yapılamaz.";
    public const string AlreadyInAnotherRoom =
        "Devam eden bir yarışmanız var. Yeni oda kurabilmek için önce onu tamamlayın veya iptal edin.";

    // --- Zamanlanmış etkinlik ------------------------------------------------
    public const string EventNotFound = "Etkinlik bulunamadı.";
    public const string EventCreated = "Etkinlik oluşturuldu.";
    public const string EventUpdated = "Etkinlik güncellendi.";
    public const string EventCancelled = "Etkinlik iptal edildi.";
    public const string EventRegistered = "Etkinliğe kaydoldunuz.";
    public const string EventWithdrawn = "Etkinlik kaydınız iptal edildi.";
    public const string EventFull = "Etkinlik kontenjanı dolu.";
    public const string EventAlreadyRegistered = "Bu etkinliğe zaten kayıtlısınız.";
    public const string EventNotRegistered = "Bu etkinliğe kayıtlı değilsiniz.";
    public const string EventAlreadyStarted = "Etkinlik başladığı için bu işlem yapılamaz.";
    public const string EventStartMustBeFuture = "Etkinlik başlangıcı gelecekte bir zaman olmalıdır.";
    public const string NotAScheduledEvent = "Bu oda zamanlanmış bir etkinlik değil.";
    public const string EventCancelledNoParticipants =
        "Etkinlik, yeterli katılımcı olmadığı için iptal edildi.";

    // --- Yarışma / oyun ------------------------------------------------------
    public const string NotEnoughQuestions =
        "Bu kategoride yeterli sayıda soru yok. Daha az soruyla deneyin veya başka bir kategori seçin.";

    public const string GameStarted = "Yarışma başladı!";
    public const string NoActiveCompetition = "Devam eden bir yarışmanız yok.";
    public const string CompetitionNotFound = "Yarışma bulunamadı.";
    public const string CompetitionAlreadyFinished = "Bu yarışma tamamlanmış.";
    public const string QuestionAlreadyAnswered = "Bu soruyu zaten cevapladınız.";
    public const string QuestionNotServedYet = "Bu soru henüz size sunulmadı.";
    public const string AnswerDoesNotBelongToQuestion = "Seçilen şık bu soruya ait değil.";
    public const string CompetitionAbandoned = "Yarışma yarıda bırakıldı.";
    public const string AnswerAccepted = "Cevabınız kaydedildi.";
    public const string CompetitionCompleted = "Yarışma tamamlandı!";

    // --- Yetki ---------------------------------------------------------------
    public const string OperationClaimNotFound = "Yetki tanımı bulunamadı.";
    public const string OperationClaimCreated = "Yetki tanımı oluşturuldu.";
    public const string OperationClaimUpdated = "Yetki tanımı güncellendi.";
    public const string OperationClaimDeleted = "Yetki tanımı kaldırıldı.";
    public const string OperationClaimNameInUse = "Bu adla bir yetki tanımı zaten var.";
    public const string ClaimAlreadyAssigned = "Bu yetki kullanıcıya zaten atanmış.";
}
