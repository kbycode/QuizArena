using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Categories;

/// <summary>
/// Kategori kartı.
/// </summary>
/// <param name="QuestionCount">
/// Yarışmaya uygun (aktif) soru sayısı. Arayüz "bu kategoride 10 soruluk
/// yarışma kurulabilir mi?" kararını buradan verir; ilk hâlde bu değer
/// <c>Category.CountQuestion</c> adlı elle güncellenen bir kolonda tutuluyordu
/// ve gerçek soru sayısıyla zamanla ayrışıyordu. Artık sorgudan türetiliyor.
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
