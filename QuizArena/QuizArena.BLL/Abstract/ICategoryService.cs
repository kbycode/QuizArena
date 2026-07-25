using QuizArena.Core.Utilities.Results;
using QuizArena.Entities.Dtos.Categories;

namespace QuizArena.BLL.Abstract;

public interface ICategoryService
{
    /// <summary>Kategori listesi (soru sayılarıyla). Önbelleklenir.</summary>
    Task<IDataResult<IReadOnlyList<CategoryResponse>>> GetListAsync(
        bool onlyActive = true,
        CancellationToken cancellationToken = default);

    Task<IDataResult<CategoryResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IDataResult<CategoryResponse>> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<IDataResult<CategoryResponse>> UpdateAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<IResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
