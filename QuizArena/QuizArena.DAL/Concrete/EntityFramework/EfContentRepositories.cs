using QuizArena.Core.DataAccess.EntityFramework;
using QuizArena.DAL.Abstract;
using QuizArena.DAL.Contexts;
using QuizArena.DAL.ReadModels;
using QuizArena.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace QuizArena.DAL.Concrete.EntityFramework;

public sealed class EfCategoryRepository
    : EfEntityRepositoryBase<Category, QuizArenaDbContext>, ICategoryRepository
{
    public EfCategoryRepository(QuizArenaDbContext context) : base(context) { }

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => GetAsync(c => c.Slug == slug, cancellationToken: cancellationToken);

    /// <remarks>
    /// Soru sayısı alt sorgu olarak <c>SELECT</c> listesine giriyor; yani
    /// kategori sayısı kaç olursa olsun <b>tek</b> veritabanı gidiş-dönüşü
    /// yapılır (N+1 yok).
    /// </remarks>
    public async Task<IReadOnlyList<CategoryWithQuestionCount>> GetWithQuestionCountsAsync(
        bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Category> query = Context.Categories.AsNoTracking();

        if (onlyActive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryWithQuestionCount(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.Icon,
                c.ColorHex,
                c.IsActive,
                c.DisplayOrder,
                // Yalnızca yarışmada çıkabilecek sorular sayılır:
                // aktif olmalı ve en az iki şıkkı bulunmalı.
                c.Questions.Count(q => q.IsActive && q.Answers.Count >= 2)))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
        => Context.Categories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(c => c.Slug == slug, cancellationToken);

    public Task<bool> NameExistsAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
        => Context.Categories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(c => c.Name == name && (excludeId == null || c.Id != excludeId), cancellationToken);
}

public sealed class EfQuestionRepository
    : EfEntityRepositoryBase<Question, QuizArenaDbContext>, IQuestionRepository
{
    public EfQuestionRepository(QuizArenaDbContext context) : base(context) { }

    public Task<Question?> GetWithAnswersAsync(
        Guid id,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
        => GetAsync(
            q => q.Id == id,
            include: q => q.Include(x => x.Answers).Include(x => x.Category),
            asNoTracking: asNoTracking,
            cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetSelectableQuestionIdsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
        => await Context.Questions
            .AsNoTracking()
            .Where(q => q.CategoryId == categoryId
                        && q.IsActive
                        // Şıkkı olmayan ya da doğru cevabı bulunmayan soru
                        // yarışmaya alınmaz: oyuncu onu asla doğru bilemez.
                        && q.Answers.Count >= 2
                        && q.Answers.Any(a => a.IsCorrect))
            .Select(q => q.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Question>> GetWithAnswersByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await Context.Questions
            .AsNoTracking()
            .Include(q => q.Answers)
            .Where(q => ids.Contains(q.Id))
            .ToListAsync(cancellationToken);
    }

    public Task IncrementAskedStatisticsAsync(
        IReadOnlyCollection<Guid> questionIds,
        CancellationToken cancellationToken = default)
    {
        if (questionIds.Count == 0)
        {
            return Task.CompletedTask;
        }

        return Context.Questions
            .Where(q => questionIds.Contains(q.Id))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(q => q.TimesAsked, q => q.TimesAsked + 1),
                cancellationToken);
    }

    public Task IncrementCorrectStatisticAsync(
        Guid questionId,
        CancellationToken cancellationToken = default)
        => Context.Questions
            .Where(q => q.Id == questionId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    q => q.TimesAnsweredCorrectly,
                    q => q.TimesAnsweredCorrectly + 1),
                cancellationToken);
}

public sealed class EfAnswerRepository
    : EfEntityRepositoryBase<Answer, QuizArenaDbContext>, IAnswerRepository
{
    public EfAnswerRepository(QuizArenaDbContext context) : base(context) { }

    public Task<IReadOnlyList<Answer>> GetByQuestionAsync(
        Guid questionId,
        CancellationToken cancellationToken = default)
        => GetListAsync(
            a => a.QuestionId == questionId,
            orderBy: q => q.OrderBy(a => a.DisplayOrder),
            cancellationToken: cancellationToken);

    /// <remarks>
    /// <b>Yumuşak silme, kalıcı değil.</b> Şık kimlikleri geçmiş yarışma
    /// cevaplarında (<c>CompetitionAnswers.SelectedAnswerId</c>) yabancı
    /// anahtar olarak duruyor. Satırlar kalıcı silinirse veritabanı bu kısıtı
    /// reddeder ve <b>bir kez oynanmış hiçbir soru düzenlenemez</b> hâle gelir.
    /// <para>
    /// İşaretlenen satırlar genel sorgu filtresi sayesinde yeni sorgularda
    /// görünmez; geçmiş cevaplar ise hangi şıkkın seçildiğini göstermeye
    /// devam eder.
    /// </para>
    /// </remarks>
    public Task DeleteByQuestionAsync(Guid questionId, CancellationToken cancellationToken = default)
        => Context.Answers
            .Where(a => a.QuestionId == questionId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(a => a.IsDeleted, true)
                    .SetProperty(a => a.DeletedAtUtc, DateTime.UtcNow),
                cancellationToken);
}
