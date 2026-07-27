namespace QuizArena.DAL.Seed;

/// <summary>
/// Yapılandırılan dile göre başlangıç içeriğini seçer.
/// </summary>
/// <remarks>
/// Seçim tek bir yerde yapılıyor; <see cref="DatabaseSeeder"/> hangi dil
/// setinin var olduğunu bilmek zorunda kalmıyor. Yeni bir dil eklendiğinde
/// değişecek tek yer burası.
/// </remarks>
internal static class SeedCatalog
{
    internal static SeedCategory[] For(SeedOptions options) => options.UseEnglishContent
        ? SeedContentEnglish.Categories
        : SeedContent.Categories;
}
