using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Categories;

/// <summary>Kategori oluşturma isteği.</summary>
public sealed record CreateCategoryRequest(
    string Name,
    string? Description,
    string? Icon,
    string? ColorHex,
    int DisplayOrder) : IDto;

/// <summary>
/// Kategori güncelleme isteği. <c>Slug</c> burada yok: bir kez üretilir ve
/// değişmez — dış bağlantıların (permalink) kırılmaması için.
/// </summary>
public sealed record UpdateCategoryRequest(
    string Name,
    string? Description,
    string? Icon,
    string? ColorHex,
    int DisplayOrder,
    bool IsActive) : IDto;
