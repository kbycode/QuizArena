namespace QuizArena.Core.Entities;

/// <summary>
/// Oluşturma/güncelleme zamanı otomatik doldurulacak varlıklar.
/// Doldurma işini <see cref="Interceptors.AuditSaveChangesInterceptor"/> yapar;
/// hiçbir servis elle tarih atamak zorunda kalmaz.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
}
