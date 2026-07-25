using QuizArena.Core.Entities;

namespace QuizArena.Entities.Concrete;

/// <summary>
/// Yarışmadaki tek bir soru adımı: hangi soru, kaçıncı sırada, ne zaman
/// soruldu, ne zaman kapanacak.
/// </summary>
/// <remarks>
/// <para>
/// <b>Bu tablonun varlık sebebi hile önlemedir.</b> Süre ölçümü istemciden
/// gelen bir sayıya bırakılırsa oyuncu "0,4 saniyede cevapladım" diyerek
/// maksimum hız bonusunu her seferinde alır. Sunucu, soruyu servis ettiği anı
/// (<see cref="AskedAtUtc"/>) ve kapanış anını (<see cref="ClosesAtUtc"/>)
/// kendisi yazar; geçen süre bu iki değere göre <b>sunucuda</b> hesaplanır.
/// </para>
/// <para>
/// İlk hâlde bu tablo <c>AnswerId</c> ve <c>Score</c> alanlarını doğrudan
/// tutuyordu; verilen cevap ile sorunun kendisi aynı satırda karışıyordu.
/// Cevap artık <see cref="CompetitionAnswer"/>'a taşındı — bir soruya birden
/// çok cevap denemesi yapılıp yapılamayacağını veri modeli belirler hâle geldi
/// (tekil kısıt sayesinde: yapılamaz).
/// </para>
/// </remarks>
public class CompetitionQuestion : EntityBase
{
    public Guid CompetitionId { get; set; }
    public Guid QuestionId { get; set; }

    /// <summary>Yarışma içindeki sıra (1'den başlar).</summary>
    public int Order { get; set; }

    /// <summary>Sorunun oyuncuya servis edildiği an. Sunucu yazar.</summary>
    public DateTime? AskedAtUtc { get; set; }

    /// <summary>
    /// Cevap kabul edilmesinin son anı = <see cref="AskedAtUtc"/> + süre limiti.
    /// Sunucu yazar; ağ gecikmesi için küçük bir tolerans doğrulama sırasında eklenir.
    /// </summary>
    public DateTime? ClosesAtUtc { get; set; }

    public Competition Competition { get; set; } = null!;
    public Question Question { get; set; } = null!;

    public CompetitionAnswer? Answer { get; set; }

    public bool IsAsked => AskedAtUtc is not null;
}
