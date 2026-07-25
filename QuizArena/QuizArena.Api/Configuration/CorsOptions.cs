namespace QuizArena.Api.Configuration;

/// <summary>
/// CORS ayarları.
/// </summary>
/// <remarks>
/// Projenin ilk hâlinde izin verilen kaynak <c>Program.cs</c> içine sabit
/// yazılmıştı (<c>WithOrigins("https://localhost:44300")</c>). Bunun anlamı:
/// uygulama başka bir ortama taşındığında <b>yeniden derlenmesi</b>
/// gerekiyordu. Ayrıca ilk kodda politika iki kez, biri isimli biri isimsiz
/// olacak şekilde tanımlanmıştı; isimsiz olan diğerini gölgeliyordu.
/// </remarks>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "QuizArenaCors";

    public string[] AllowedOrigins { get; set; } = [];
}
