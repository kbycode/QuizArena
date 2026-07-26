using QuizArena.Core.Utilities.Results;
using QuizArena.Entities.Dtos.Admin;

namespace QuizArena.BLL.Abstract;

/// <summary>Yönetim panosu (salt okunur özet).</summary>
public interface IDashboardService
{
    /// <summary>
    /// Panonun tüm bloklarını tek çağrıda döner.
    /// </summary>
    /// <param name="windowDays">
    /// "Son N gün" sayaçları ve günlük grafik için pencere genişliği.
    /// İş katmanında makul bir aralığa sıkıştırılır.
    /// </param>
    Task<IDataResult<DashboardResponse>> GetAsync(
        int windowDays,
        CancellationToken cancellationToken = default);
}
