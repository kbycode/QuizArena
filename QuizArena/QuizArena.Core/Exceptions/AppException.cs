namespace QuizArena.Core.Exceptions;

/// <summary>
/// Uygulamanın bilinçli olarak attığı istisnaların ortak atası.
/// </summary>
/// <remarks>
/// Bu hiyerarşinin varlık sebebi: middleware'in <c>catch</c> bloğunda
/// istisna tipine göre HTTP durum kodu seçebilmek. Böylece hiçbir servis
/// <c>StatusCode</c> bilmek zorunda kalmaz — HTTP bilgisi API katmanında kalır.
/// <para>
/// <see cref="AppException"/> türevleri <b>beklenen</b> hatalardır: istemcinin
/// düzeltebileceği veya bilmesi gereken durumlar. Bunlar loglarda
/// <c>Warning</c>'dir. Türemeyen her istisna <b>beklenmeyen</b> kabul edilir:
/// <c>Error</c> olarak loglanır ve istemciye ayrıntısı sızdırılmaz.
/// </para>
/// </remarks>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }

    protected AppException(string message, Exception innerException)
        : base(message, innerException) { }
}
