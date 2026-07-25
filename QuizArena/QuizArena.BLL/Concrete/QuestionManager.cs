using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.BLL.Mapping;
using QuizArena.BLL.Validation;
using QuizArena.Core.Aspects.Authorization;
using QuizArena.Core.Aspects.Caching;
using QuizArena.Core.Aspects.Transaction;
using QuizArena.Core.Aspects.Validation;
using QuizArena.Core.DataAccess.Paging;
using QuizArena.Core.Exceptions;
using QuizArena.Core.Utilities.Results;
using QuizArena.DAL.Abstract;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Dtos.Common;
using QuizArena.Entities.Dtos.Questions;
using Microsoft.EntityFrameworkCore;

namespace QuizArena.BLL.Concrete;

/// <summary>
/// Soru yönetimi.
/// </summary>
/// <remarks>
/// <para>
/// <b>Tüm metotlar yetki gerektirir.</b> Sebep sadece "yönetim işlemi olması"
/// değil: bu servisin döndürdüğü <see cref="QuestionResponse"/> doğru cevap
/// bilgisini içerir. Yetkisiz bir kullanıcı listeleme ucuna erişebilse
/// yarışmanın tüm cevap anahtarını indirebilirdi.
/// </para>
/// <para>
/// Soru ve şıkları <b>tek işlemde</b> kaydedilir; yarım kalmış (şıksız) soru
/// oluşması yapısal olarak engellenir.
/// </para>
/// </remarks>
public sealed class QuestionManager : IQuestionService
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly ICategoryRepository _categoryRepository;

    public QuestionManager(
        IQuestionRepository questionRepository,
        IAnswerRepository answerRepository,
        ICategoryRepository categoryRepository)
    {
        _questionRepository = questionRepository;
        _answerRepository = answerRepository;
        _categoryRepository = categoryRepository;
    }

    [SecuredOperationAspect(Roles.Admin, Roles.QuestionManage)]
    public async Task<IDataResult<PagedResponse<QuestionResponse>>> GetPagedAsync(
        PageRequest pageRequest,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        PagedList<Question> page = await _questionRepository.GetPagedListAsync(
            pageRequest,
            predicate: categoryId is null ? null : q => q.CategoryId == categoryId,
            orderBy: q => q.OrderByDescending(x => x.CreatedAtUtc),
            include: q => q.Include(x => x.Answers).Include(x => x.Category),
            cancellationToken: cancellationToken);

        PagedList<QuestionResponse> mapped = page.Map(q => q.ToAdminResponse());

        return new SuccessDataResult<PagedResponse<QuestionResponse>>(PagedResponse<QuestionResponse>.From(mapped));
    }

    [SecuredOperationAspect(Roles.Admin, Roles.QuestionManage)]
    public async Task<IDataResult<QuestionResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        Question question = await _questionRepository.GetWithAnswersAsync(id, cancellationToken: cancellationToken)
                            ?? throw new NotFoundException(Messages.QuestionNotFound);

        return new SuccessDataResult<QuestionResponse>(question.ToAdminResponse());
    }

    [SecuredOperationAspect(Roles.Admin, Roles.QuestionManage)]
    [ValidationAspect(typeof(CreateQuestionRequestValidator))]
    [TransactionAspect]
    [CacheRemoveAspect(CacheKeys.Questions, CacheKeys.Categories)]
    public async Task<IDataResult<QuestionResponse>> CreateAsync(
        CreateQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

        var question = new Question
        {
            CategoryId = request.CategoryId,
            Text = request.Text.Trim(),
            Difficulty = request.Difficulty,
            TimeLimitSeconds = request.TimeLimitSeconds,
            Explanation = request.Explanation?.Trim(),
            IsActive = true
        };

        foreach (SaveAnswerRequest answer in request.Answers)
        {
            question.Answers.Add(new Answer
            {
                QuestionId = question.Id,
                Text = answer.Text.Trim(),
                IsCorrect = answer.IsCorrect,
                DisplayOrder = answer.DisplayOrder
            });
        }

        await _questionRepository.AddAsync(question, cancellationToken);

        Question saved = await _questionRepository.GetWithAnswersAsync(question.Id, cancellationToken: cancellationToken)
                         ?? question;

        return new SuccessDataResult<QuestionResponse>(saved.ToAdminResponse(), Messages.QuestionCreated);
    }

    [SecuredOperationAspect(Roles.Admin, Roles.QuestionManage)]
    [ValidationAspect(typeof(UpdateQuestionRequestValidator))]
    [TransactionAspect]
    [CacheRemoveAspect(CacheKeys.Questions, CacheKeys.Categories)]
    public async Task<IDataResult<QuestionResponse>> UpdateAsync(
        Guid id,
        UpdateQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        Question question = await _questionRepository.GetAsync(q => q.Id == id, asNoTracking: false,
                                cancellationToken: cancellationToken)
                            ?? throw new NotFoundException(Messages.QuestionNotFound);

        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

        question.CategoryId = request.CategoryId;
        question.Text = request.Text.Trim();
        question.Difficulty = request.Difficulty;
        question.TimeLimitSeconds = request.TimeLimitSeconds;
        question.Explanation = request.Explanation?.Trim();
        question.IsActive = request.IsActive;

        await _questionRepository.UpdateAsync(question, cancellationToken);

        // Şıklar tümüyle yenilenir.
        //
        // Neden "eşleştirip güncelle" değil: şık kimlikleri geçmiş yarışma
        // cevaplarında (CompetitionAnswers.SelectedAnswerId) referans olarak
        // durur. Mevcut bir şıkkın metnini/doğruluğunu değiştirmek, geçmiş
        // yarışmaların sonucunu geçmişe dönük değiştirmek olurdu. Eski şıklar
        // silinip yenileri eklendiğinde geçmiş cevaplar kendi kayıtlarındaki
        // puanı korur.
        await _answerRepository.DeleteByQuestionAsync(id, cancellationToken);

        var answers = request.Answers
            .Select(a => new Answer
            {
                QuestionId = id,
                Text = a.Text.Trim(),
                IsCorrect = a.IsCorrect,
                DisplayOrder = a.DisplayOrder
            })
            .ToList();

        await _answerRepository.AddRangeAsync(answers, cancellationToken);

        Question saved = await _questionRepository.GetWithAnswersAsync(id, cancellationToken: cancellationToken)
                         ?? question;

        return new SuccessDataResult<QuestionResponse>(saved.ToAdminResponse(), Messages.QuestionUpdated);
    }

    [SecuredOperationAspect(Roles.Admin, Roles.QuestionManage)]
    [CacheRemoveAspect(CacheKeys.Questions, CacheKeys.Categories)]
    public async Task<IResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Question question = await _questionRepository.GetAsync(q => q.Id == id, asNoTracking: false,
                                cancellationToken: cancellationToken)
                            ?? throw new NotFoundException(Messages.QuestionNotFound);

        // Yumuşak silme: soru geçmiş yarışmalarda referanslı.
        await _questionRepository.DeleteAsync(question, cancellationToken: cancellationToken);

        return new SuccessResult(Messages.QuestionDeleted);
    }

    private async Task EnsureCategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        if (!await _categoryRepository.AnyAsync(c => c.Id == categoryId, cancellationToken))
        {
            throw new NotFoundException(Messages.CategoryNotFound);
        }
    }
}
