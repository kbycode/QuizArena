using QuizArena.Core.DataAccess.Paging;
using QuizArena.Core.Utilities.Results;
using QuizArena.Entities.Dtos.Common;
using QuizArena.Entities.Dtos.Play;

namespace QuizArena.BLL.Abstract;

/// <summary>
/// Yarışma akışı: soru sun, cevabı doğrula, puanla, bitir.
/// </summary>
/// <remarks>
/// Bu servis, projenin ilk hâlinde <b>hiç var olmayan</b> parçadır. Önceki
/// kodda "yarışma" yalnızca CRUD tabloları olarak duruyordu; soruyu kimin
/// hangi sırayla göreceği, sürenin nasıl ölçüleceği ve puanın nasıl
/// hesaplanacağı hiçbir yerde tanımlı değildi.
/// </remarks>
public interface IGameService
{
    /// <summary>
    /// Oyuncunun cevaplaması gereken sıradaki soruyu döner.
    /// </summary>
    /// <remarks>
    /// Soru ilk kez sunulduğunda sunucu, sunum ve kapanış zamanını
    /// <b>kendisi</b> kaydeder; süre ölçümünün tek doğruluk kaynağı budur.
    /// Süresi geçmiş bir soru varsa önce o "süre doldu" olarak kapatılır,
    /// sonra sıradaki soru sunulur. Yarışma bittiyse <c>Data</c> boş döner.
    /// </remarks>
    Task<IDataResult<QuizQuestionResponse?>> GetCurrentQuestionAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Cevabı doğrular, puanlar ve sonucu döner.</summary>
    Task<IDataResult<AnswerResultResponse>> SubmitAnswerAsync(
        SubmitAnswerRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Tamamlanmış yarışmanın sonuç ekranı.</summary>
    Task<IDataResult<CompetitionSummaryResponse>> GetSummaryAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default);

    /// <summary>Devam eden yarışmayı yarıda bırakır.</summary>
    Task<IResult> AbandonAsync(CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının tamamladığı yarışmaların geçmişi.</summary>
    Task<IDataResult<PagedResponse<CompetitionHistoryResponse>>> GetMyHistoryAsync(
        PageRequest pageRequest,
        CancellationToken cancellationToken = default);
}
