using QuizArena.Core.Entities;

namespace QuizArena.Entities.Concrete;

/// <summary>Soru kategorisi (Tarih, Coğrafya, Bilim…).</summary>
public class Category : EntityBase
{
    public string Name { get; set; } = null!;

    /// <summary>URL'de kullanılabilir kısa ad. Örn. <c>genel-kultur</c>.</summary>
    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Arayüzde kategoriyi temsil eden emoji/ikon.</summary>
    public string? Icon { get; set; }

    /// <summary>Arayüzde vurgu rengi (hex).</summary>
    public string? ColorHex { get; set; }

    /// <summary>
    /// Kapatılan kategori yeni yarışmalarda çıkmaz ama geçmiş sonuçlar korunur.
    /// (Silmek yerine kapatmak, rapor tutarlılığı için tercih edilir.)
    /// </summary>
    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public ICollection<Question> Questions { get; set; } = [];
    public ICollection<Room> Rooms { get; set; } = [];
}
