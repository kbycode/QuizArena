using QuizArena.Core.Utilities.Results;
using QuizArena.Entities.Dtos.Admin;

namespace QuizArena.BLL.Abstract;

/// <summary>Yetki tanımları ve kullanıcı-yetki atamaları (yönetici).</summary>
public interface IOperationClaimService
{
    Task<IDataResult<IReadOnlyList<OperationClaimResponse>>> GetListAsync(
        CancellationToken cancellationToken = default);

    Task<IDataResult<OperationClaimResponse>> CreateAsync(
        SaveOperationClaimRequest request,
        CancellationToken cancellationToken = default);

    Task<IDataResult<OperationClaimResponse>> UpdateAsync(
        Guid id,
        SaveOperationClaimRequest request,
        CancellationToken cancellationToken = default);

    Task<IResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IResult> AssignToUserAsync(
        AssignOperationClaimRequest request,
        CancellationToken cancellationToken = default);

    Task<IResult> RevokeFromUserAsync(
        AssignOperationClaimRequest request,
        CancellationToken cancellationToken = default);
}
