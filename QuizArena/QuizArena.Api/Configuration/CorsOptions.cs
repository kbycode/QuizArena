namespace QuizArena.Api.Configuration;

/// <summary>
/// CORS ayarları.
/// </summary>
/// <remarks>
/// İzin verilen kaynaklar yapılandırmadan okunur, koda gömülmez: sabit
/// yazılmış bir <c>WithOrigins(...)</c>, uygulamayı başka bir ortama taşımak
/// için <b>yeniden derleme</b> gerektirirdi.
/// <para>
/// Politika tek ve <b>isimli</b> tanımlanır. İsimli ve isimsiz politika bir
/// arada tanımlanırsa isimsiz olan diğerini sessizce gölgeler; sonuç, açık
/// görünen ama uygulanmayan bir CORS kuralıdır.
/// </para>
/// </remarks>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "QuizArenaCors";

    public string[] AllowedOrigins { get; set; } = [];
}
