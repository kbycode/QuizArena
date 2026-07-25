namespace QuizArena.BLL.Constants;

/// <summary>
/// Önbellek anahtar önekleri.
/// </summary>
/// <remarks>
/// Önekler sabit olarak tutuluyor çünkü <c>[CacheAspect]</c> ile
/// <c>[CacheRemoveAspect]</c>'in <b>aynı</b> öneki kullanması zorunlu.
/// Biri "Category", diğeri "Categories" yazsaydı önbellek hiç temizlenmez ve
/// kullanıcı güncellemeden sonra dakikalarca eski veriyi görürdü — sessiz,
/// bulması zor bir hata.
/// </remarks>
public static class CacheKeys
{
    public const string Categories = "Categories";
    public const string Questions = "Questions";
    public const string Leaderboard = "Leaderboard";
    public const string Achievements = "Achievements";
}
