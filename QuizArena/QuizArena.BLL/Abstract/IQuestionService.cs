using QuizArena.Core.DataAccess.Paging;
using QuizArena.Core.Utilities.Results;
using QuizArena.Entities.Dtos.Common;
using QuizArena.Entities.Dtos.Questions;

namespace QuizArena.BLL.Abstract;

/// <summary>
/// Soru yönetimi. <b>Tüm metotları yetki gerektirir</b> — döndürdüğü DTO
/// doğru cevap bilgisini içerdiği için oyunculara açılmaz.
/// </summary>
public interface IQuestionService
{
    Task<IDataResult<PagedResponse<QuestionResponse>>> GetPagedAsync(
        PageRequest pageRequest,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);

    Task<IDataResult<QuestionResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IDataResult<QuestionResponse>> CreateAsync(
        CreateQuestionRequest request,
        CancellationToken cancellationToken = default);

    Task<IDataResult<QuestionResponse>> UpdateAsync(
        Guid id,
        UpdateQuestionRequest request,
        CancellationToken cancellationToken = default);

    Task<IResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
