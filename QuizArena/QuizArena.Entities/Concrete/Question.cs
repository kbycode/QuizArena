using QuizArena.Core.Entities;
using QuizArena.Entities.Enums;

namespace QuizArena.Entities.Concrete;

/// <summary>Soru.</summary>
/// <remarks>
/// İlk hâlde alan adı <c>TheQuestion</c>, seçenek sayısı ise
/// <c>CountOptions</c> adlı elle güncellenen bir <c>int</c> idi. Sayacı elle
/// tutmak, kayıtla gerçek seçenek sayısının zamanla ayrışmasına (veri
/// tutarsızlığı) yol açar; sayı artık <see cref="Answers"/> koleksiyonundan
/// türetilir.
/// </remarks>
public class Question : EntityBase
{
    public Guid CategoryId { get; set; }

    public string Text { get; set; } = null!;

    public QuestionDifficulty Difficulty { get; set; } = QuestionDifficulty.Medium;

    /// <summary>
    /// Bu soru için <b>önerilen</b> süre (saniye).
    /// </summary>
    /// <remarks>
    /// Yarışma sırasında geçerli olan süre, oyuncunun oda kurarken seçtiği
    /// <c>Room.SecondsPerQuestion</c> değeridir. Buradaki değer, soru
    /// yönetiminde "bu soru ne kadar süre gerektirir?" bilgisini taşır ve oda
    /// bağlamı olmayan durumlarda varsayılan olarak kullanılır.
    /// <para>
    /// Oda ayarının kazanması bilinçli bir karardır: oyuncu 10 saniyelik hızlı
    /// bir tur istediyse, tek bir sorunun bunu 25 saniyeye çıkarması onun
    /// tercihini geçersiz kılardı.
    /// </para>
    /// </remarks>
    public int TimeLimitSeconds { get; set; } = 20;

    /// <summary>
    /// Cevaplandıktan sonra oyuncuya gösterilen açıklama.
    /// Yarışmayı öğretici hâle getirir.
    /// </summary>
    public string? Explanation { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Bu sorunun kaç kez soruldu (istatistik/zorluk kalibrasyonu).</summary>
    public int TimesAsked { get; set; }

    /// <summary>Kaç kez doğru cevaplandı. Gerçek zorluk oranını verir.</summary>
    public int TimesAnsweredCorrectly { get; set; }

    public Category Category { get; set; } = null!;

    public ICollection<Answer> Answers { get; set; } = [];
}
