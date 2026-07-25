using QuizArena.Core.Utilities.Ids;

namespace QuizArena.Core.Entities;

/// <summary>
/// Tüm varlıkların ortak taban sınıfı: kimlik + denetim alanları + yumuşak silme.
/// </summary>
public abstract class EntityBase : IEntity, IAuditable, ISoftDeletable
{
    /// <summary>
    /// Sıralı (sequential) GUID ile başlatılır.
    /// <para>
    /// Neden rastgele <c>Guid.NewGuid()</c> değil: SQL Server'da birincil anahtar
    /// kümelenmiş (clustered) indekstir. Rastgele GUID her ekleme işleminde
    /// indeksin ortasına yazar; sayfa bölünmesi (page split) ve fragmentasyon
    /// üretir. <see cref="SequentialGuid"/> artan değer ürettiği için ekleme
    /// hep sonda olur.
    /// </para>
    /// </summary>
    public Guid Id { get; set; } = SequentialGuid.NewGuid();

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
