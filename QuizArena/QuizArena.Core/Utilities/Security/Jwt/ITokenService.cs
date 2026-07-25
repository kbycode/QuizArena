using QuizArena.Core.Entities.Concrete;

namespace QuizArena.Core.Utilities.Security.Jwt;

/// <summary>Jeton üretme/özetleme soyutlaması.</summary>
public interface ITokenService
{
    /// <summary>
    /// Kullanıcı ve yetkileri için imzalı JWT + ham yenileme jetonu üretir.
    /// </summary>
    AccessToken CreateAccessToken(User user, IEnumerable<string> operationClaims);

    /// <summary>
    /// Ham yenileme jetonunun veritabanında saklanacak SHA-256 özetini üretir.
    /// Aynı girdi için hep aynı çıktıyı verir (aranabilir olması gerekir),
    /// bu yüzden tuzlanmaz — jeton zaten 256 bit kriptografik rastgeleliktir,
    /// sözlük saldırısına konu olamaz.
    /// </summary>
    string HashRefreshToken(string refreshToken);
}
