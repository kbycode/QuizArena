namespace QuizArena.Core.Utilities.Security.Hashing;

/// <summary>Parola doğrulama sonucu.</summary>
public enum PasswordVerificationResult
{
    /// <summary>Parola yanlış.</summary>
    Failed = 0,

    /// <summary>Parola doğru, saklanan özet güncel parametrelerle üretilmiş.</summary>
    Success = 1,

    /// <summary>
    /// Parola doğru, ancak özet eski parametrelerle (ör. daha az iterasyon)
    /// üretilmiş. Çağıran taraf parolayı elinde tutuyorken sessizce yeniden
    /// özetleyip kaydetmelidir — kullanıcı hiçbir şey fark etmez.
    /// </summary>
    SuccessRehashNeeded = 2
}

/// <summary>
/// Parola özetleme soyutlaması. Algoritma değiştiğinde iş katmanında
/// tek satır kod değişmez.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Parolayı, parametrelerini içinde taşıyan tek bir dizeye çevirir.</summary>
    string Hash(string password);

    /// <summary>Parolayı saklanan özetle karşılaştırır.</summary>
    PasswordVerificationResult Verify(string password, string encodedHash);
}
