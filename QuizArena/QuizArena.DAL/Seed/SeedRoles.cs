namespace QuizArena.DAL.Seed;

/// <summary>
/// Sistemin tanıdığı işlem yetkileri.
/// </summary>
/// <remarks>
/// <para>
/// Yetki adları hem seed hem de <c>[Authorize(Roles = ...)]</c> ve
/// <c>[SecuredOperationAspect(...)]</c> tarafından kullanılır. Tek bir yerde
/// sabit olarak tutulmaları kritik: yazım hatası olan bir rol adı
/// <c>[Authorize(Roles = "Admn")]</c> hiçbir zaman eşleşmez ve <b>sessizce
/// herkesi reddeder</b> — ya da daha kötüsü, kontrolü hiç yapmaz gibi görünür.
/// Derleyici sabit üzerinden yazım hatasını yakalar.
/// </para>
/// <para>
/// BLL katmanındaki <c>Roles</c> sınıfı bu değerleri yeniden bildirmez;
/// aynı sabitleri kullanır.
/// </para>
/// </remarks>
public static class SeedRoles
{
    public const string Admin = "Admin";
    public const string CategoryManage = "Category.Manage";
    public const string QuestionManage = "Question.Manage";
    public const string UserManage = "User.Manage";
    public const string EventManage = "Event.Manage";

    /// <summary>Yetki adı → açıklama.</summary>
    public static IReadOnlyDictionary<string, string> All { get; } = new Dictionary<string, string>
    {
        [Admin] = "Tam yönetici yetkisi.",
        [CategoryManage] = "Kategori ekleme, güncelleme ve kapatma.",
        [QuestionManage] = "Soru ve şık yönetimi.",
        [UserManage] = "Kullanıcı listeleme, hesap kilidi açma ve yetki atama.",
        [EventManage] = "Zamanlanmış etkinlik oluşturma ve yönetme."
    };
}
