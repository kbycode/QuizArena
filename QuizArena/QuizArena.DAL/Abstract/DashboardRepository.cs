using QuizArena.DAL.ReadModels;

namespace QuizArena.DAL.Abstract;

/// <summary>
/// Yönetim panosunun toplama (aggregate) sorguları.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden mevcut repository'lere dağıtılmadı?</b> Panonun her sorusu birden
/// çok tabloyu birden ilgilendiriyor: "kategori başına doğruluk oranı" hem
/// <c>Categories</c> hem <c>Questions</c> hem de <c>Rooms</c> tablosunu okur.
/// Bu metotları <c>ICategoryRepository</c>'ye koymak, o arayüzü kategoriyle
/// ilgisi olmayan sorumluluklarla şişirirdi.
/// </para>
/// <para>
/// Buradaki hiçbir metot varlık (entity) döndürmez; hepsi okuma modeli
/// döndürür. Pano <b>salt okunur</b> bir yüzeydir ve yazma yolu yoktur —
/// arayüz de bunu yansıtır.
/// </para>
/// </remarks>
public interface IDashboardRepository
{
    /// <summary>
    /// Özet sayaçlar.
    /// </summary>
    /// <param name="windowStartUtc">
    /// "Son N gün" sayaçlarının başlangıcı (<c>NewUsersInWindow</c>,
    /// <c>CompetitionsInWindow</c>).
    /// </param>
    Task<DashboardCounters> GetCountersAsync(
        DateTime windowStartUtc,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Günlük etkinlik: tamamlanan yarışma ve benzersiz oyuncu sayısı.
    /// </summary>
    /// <remarks>
    /// Veri olmayan günler <b>dönmez</b>; boşlukları doldurmak iş katmanının
    /// işidir. Veri katmanı "hangi günlerde ne olmuş" sorusunu yanıtlar,
    /// "grafik kaç sütun göstermeli" sorusunu değil.
    /// </remarks>
    Task<IReadOnlyList<DailyActivityRow>> GetDailyActivityAsync(
        DateTime fromUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Kategori kırılımı.</summary>
    Task<IReadOnlyList<CategoryBreakdownRow>> GetCategoryBreakdownAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Başarı oranına göre sıralanmış sorular.
    /// </summary>
    /// <param name="ascending">
    /// <c>true</c> ise en zorlar (düşük başarı), <c>false</c> ise en kolaylar.
    /// </param>
    /// <param name="minimumTimesAsked">
    /// Bu sayıdan az sorulmuş sorular listelenmez. <b>Gerekçe:</b> bir kez
    /// sorulup bilinememiş bir soru "%0 başarı" ile listenin tepesine
    /// otururdu; oysa örneklem tek gözlemden ibarettir. Eşik, istatistiksel
    /// olarak anlamsız satırları eler.
    /// </param>
    Task<IReadOnlyList<QuestionStatisticRow>> GetQuestionsBySuccessRateAsync(
        int top,
        bool ascending,
        int minimumTimesAsked,
        CancellationToken cancellationToken = default);
}
