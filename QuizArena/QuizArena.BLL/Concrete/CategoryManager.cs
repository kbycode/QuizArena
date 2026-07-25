using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.BLL.Mapping;
using QuizArena.BLL.Utilities;
using QuizArena.BLL.Validation;
using QuizArena.Core.Aspects.Authorization;
using QuizArena.Core.Aspects.Caching;
using QuizArena.Core.Aspects.Validation;
using QuizArena.Core.Exceptions;
using QuizArena.Core.Utilities.Results;
using QuizArena.DAL.Abstract;
using QuizArena.DAL.ReadModels;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Dtos.Categories;

namespace QuizArena.BLL.Concrete;

/// <summary>Kategori yönetimi.</summary>
public sealed class CategoryManager : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IQuestionRepository _questionRepository;

    public CategoryManager(ICategoryRepository categoryRepository, IQuestionRepository questionRepository)
    {
        _categoryRepository = categoryRepository;
        _questionRepository = questionRepository;
    }

    /// <remarks>
    /// Kategoriler neredeyse hiç değişmeyen ama <b>her oyun başlangıcında
    /// okunan</b> veridir; önbelleklemeye en uygun aday. Değişiklik yapan
    /// metotlar <c>[CacheRemoveAspect]</c> ile aynı öneki temizler, böylece
    /// güncelleme sonrası bayat veri görünmez.
    /// </remarks>
    [CacheAspect(KeyPrefix = CacheKeys.Categories, DurationMinutes = GameRules.CategoryCacheMinutes)]
    public async Task<IDataResult<IReadOnlyList<CategoryResponse>>> GetListAsync(
        bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CategoryWithQuestionCount> categories =
            await _categoryRepository.GetWithQuestionCountsAsync(onlyActive, cancellationToken);

        IReadOnlyList<CategoryResponse> response = categories.Select(c => c.ToResponse()).ToArray();

        return new SuccessDataResult<IReadOnlyList<CategoryResponse>>(response);
    }

    public async Task<IDataResult<CategoryResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        Category category = await _categoryRepository.GetAsync(c => c.Id == id, cancellationToken: cancellationToken)
                            ?? throw new NotFoundException(Messages.CategoryNotFound);

        int questionCount = await _questionRepository.CountAsync(
            q => q.CategoryId == id && q.IsActive, cancellationToken);

        return new SuccessDataResult<CategoryResponse>(category.ToResponse(questionCount));
    }

    [SecuredOperationAspect(Roles.Admin, Roles.CategoryManage)]
    [ValidationAspect(typeof(CreateCategoryRequestValidator))]
    [CacheRemoveAspect(CacheKeys.Categories)]
    public async Task<IDataResult<CategoryResponse>> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        string name = request.Name.Trim();

        if (await _categoryRepository.NameExistsAsync(name, cancellationToken: cancellationToken))
        {
            throw new ConflictException(Messages.CategoryNameInUse);
        }

        var category = new Category
        {
            Name = name,
            Slug = await GenerateUniqueSlugAsync(name, cancellationToken),
            Description = request.Description?.Trim(),
            Icon = request.Icon?.Trim(),
            ColorHex = request.ColorHex?.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsActive = true
        };

        await _categoryRepository.AddAsync(category, cancellationToken);

        return new SuccessDataResult<CategoryResponse>(category.ToResponse(0), Messages.CategoryCreated);
    }

    [SecuredOperationAspect(Roles.Admin, Roles.CategoryManage)]
    [ValidationAspect(typeof(UpdateCategoryRequestValidator))]
    [CacheRemoveAspect(CacheKeys.Categories)]
    public async Task<IDataResult<CategoryResponse>> UpdateAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        Category category = await _categoryRepository.GetAsync(c => c.Id == id, asNoTracking: false,
                                cancellationToken: cancellationToken)
                            ?? throw new NotFoundException(Messages.CategoryNotFound);

        string name = request.Name.Trim();

        if (!string.Equals(category.Name, name, StringComparison.Ordinal) &&
            await _categoryRepository.NameExistsAsync(name, id, cancellationToken))
        {
            throw new ConflictException(Messages.CategoryNameInUse);
        }

        category.Name = name;
        category.Description = request.Description?.Trim();
        category.Icon = request.Icon?.Trim();
        category.ColorHex = request.ColorHex?.Trim();
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;

        // Slug bilinçli olarak güncellenmiyor: paylaşılmış bağlantıların
        // (permalink) kırılmaması için bir kez üretilip sabit kalır.

        await _categoryRepository.UpdateAsync(category, cancellationToken);

        int questionCount = await _questionRepository.CountAsync(
            q => q.CategoryId == id && q.IsActive, cancellationToken);

        return new SuccessDataResult<CategoryResponse>(
            category.ToResponse(questionCount),
            Messages.CategoryUpdated);
    }

    [SecuredOperationAspect(Roles.Admin, Roles.CategoryManage)]
    [CacheRemoveAspect(CacheKeys.Categories, CacheKeys.Questions)]
    public async Task<IResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Category category = await _categoryRepository.GetAsync(c => c.Id == id, asNoTracking: false,
                                cancellationToken: cancellationToken)
                            ?? throw new NotFoundException(Messages.CategoryNotFound);

        // Yumuşak silme: geçmiş yarışma sonuçları ve istatistikler kategoriye
        // bağlı olduğu için kayıt fiziksel olarak silinmez.
        await _categoryRepository.DeleteAsync(category, cancellationToken: cancellationToken);

        return new SuccessResult(Messages.CategoryDeleted);
    }

    /// <summary>
    /// Benzersiz slug üretir; çakışma varsa sonuna sayı ekler
    /// (<c>tarih</c> → <c>tarih-2</c>).
    /// </summary>
    private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken cancellationToken)
    {
        string baseSlug = SlugGenerator.Generate(name);

        if (string.IsNullOrEmpty(baseSlug))
        {
            // Ad tamamen sembollerden oluşuyorsa (ör. "###") slug boş kalır.
            baseSlug = "kategori";
        }

        string slug = baseSlug;
        var suffix = 2;

        // Silinmiş kayıtlar da slug'ı tuttuğu için filtre atlayan kontrol.
        while (await _categoryRepository.SlugExistsAsync(slug, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }
}
