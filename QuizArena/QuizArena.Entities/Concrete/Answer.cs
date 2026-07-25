using QuizArena.Core.Entities;

namespace QuizArena.Entities.Concrete;

/// <summary>Bir soruya ait seçenek.</summary>
/// <remarks>
/// <b>Güvenlik açısından bu sınıfın en kritik alanı <see cref="IsCorrect"/>.</b>
/// Projenin ilk hâlinde <c>AnswersController</c> bu varlığı doğrudan JSON
/// olarak döndürüyordu; yani istemci soruları çekerken <b>doğru cevabı da
/// alıyordu</b> — yarışmanın tüm anlamı ortadan kalkıyordu. Artık soru servis
/// eden uçlar yalnızca <c>QuizAnswerResponse</c> DTO'sunu döner ve o DTO'da
/// böyle bir alan <b>yoktur</b>.
/// </remarks>
public class Answer : EntityBase
{
    public Guid QuestionId { get; set; }

    public string Text { get; set; } = null!;

    /// <summary>Doğru cevap mı? İstemciye asla bu varlıkla birlikte gönderilmez.</summary>
    public bool IsCorrect { get; set; }

    /// <summary>Seçeneklerin gösterim sırası (A, B, C, D).</summary>
    public int DisplayOrder { get; set; }

    public Question Question { get; set; } = null!;
}
