namespace QuizArena.Core.Entities;

/// <summary>
/// Kalıcı silme yerine "silindi" işareti konan varlıklar.
/// <para>
/// Neden: Bir kategori silindiğinde ona bağlı geçmiş yarışma sonuçları
/// anlamsızlaşmasın; rapor ve sıralama tabloları geçmişe dönük tutarlı kalsın.
/// DbContext'teki global sorgu filtresi bu varlıkları otomatik gizler.
/// </para>
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
}
