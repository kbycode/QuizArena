using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Categories;

/// <summary>
/// Kategori kartı.
/// </summary>
/// <param name="QuestionCount">
/// Yarışmaya uygun (aktif) soru sayısı. Arayüz "bu kategoride 10 soruluk
/// yarışma kurulabilir mi?" kararını buradan verir. Değer <b>sorgudan
/// türetilir</b>, tabloda tutulmaz: elle güncellenen bir sayaç kolonu er ya
/// da geç gerçek soru sayısıyla ayrışır ve arayüz olmayan soruya güvenir.
/// </param>
public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Icon,
    string? ColorHex,
    bool IsActive,
    int DisplayOrder,
    int QuestionCount) : IDto;
