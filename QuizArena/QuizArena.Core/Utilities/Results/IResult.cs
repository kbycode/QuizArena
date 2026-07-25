namespace QuizArena.Core.Utilities.Results;

/// <summary>
/// İş katmanının dönüş sözleşmesi: "işlem oldu mu, olmadıysa mesajı ne?".
/// <para>
/// Neden <c>bool</c> veya <c>throw</c> değil: <c>bool</c> sebebi taşımaz,
/// exception ise <b>beklenen</b> iş sonuçları için pahalı ve yanlış araçtır
/// ("bu e-posta zaten kayıtlı" bir hata değil, geçerli bir iş sonucudur).
/// Gerçekten istisnai durumlar için <see cref="Exceptions"/> altındaki
/// tipler kullanılır.
/// </para>
/// </summary>
public interface IResult
{
    bool Success { get; }
    string Message { get; }
}
