namespace QuizArena.Core.Utilities.Security.Jwt;

/// <summary>Projeye özgü claim adları.</summary>
public static class AppClaimTypes
{
    /// <summary>
    /// Kullanıcının güvenlik damgası. Parola değiştiğinde/hesap kilitlendiğinde
    /// damga yenilenir ve elde dolaşan eski JWT'ler geçersizleşir.
    /// </summary>
    public const string SecurityStamp = "sstamp";

    /// <summary>Ekranlarda gösterilecek takma ad.</summary>
    public const string Nickname = "nickname";
}
