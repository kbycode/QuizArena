using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.BLL.Mapping;
using QuizArena.BLL.Validation;
using QuizArena.Core.Aspects.Authorization;
using QuizArena.Core.Aspects.Validation;
using QuizArena.Core.Entities.Concrete;
using QuizArena.Core.Exceptions;
using QuizArena.Core.Utilities.Results;
using QuizArena.Core.Utilities.Security;
using QuizArena.DAL.Abstract;
using QuizArena.Entities.Dtos.Admin;

namespace QuizArena.BLL.Concrete;

/// <summary>
/// Yetki tanımları ve kullanıcı-yetki atamaları.
/// </summary>
/// <remarks>
/// Bu servisin tamamı <c>Admin</c> yetkisi ister. İlk hâlde
/// <c>OperationClaimsController</c> ve <c>UserOperationClaimsController</c>
/// <b>hiçbir yetkilendirme olmadan</b> açıktı; yani kimliği doğrulanmamış
/// herhangi bir istemci <c>POST /api/useroperationclaims/add</c> ile kendisine
/// <c>Admin</c> yetkisi atayabilirdi. Bu, projedeki en ağır güvenlik açığıydı:
/// tek istekle tam yönetici olmak mümkündü.
/// </remarks>
public sealed class OperationClaimManager : IOperationClaimService
{
    private readonly IOperationClaimRepository _operationClaimRepository;
    private readonly IUserOperationClaimRepository _userOperationClaimRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public OperationClaimManager(
        IOperationClaimRepository operationClaimRepository,
        IUserOperationClaimRepository userOperationClaimRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser)
    {
        _operationClaimRepository = operationClaimRepository;
        _userOperationClaimRepository = userOperationClaimRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    [SecuredOperationAspect(Roles.Admin)]
    public async Task<IDataResult<IReadOnlyList<OperationClaimResponse>>> GetListAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OperationClaim> claims = await _operationClaimRepository.GetListAsync(
            orderBy: q => q.OrderBy(c => c.Name),
            cancellationToken: cancellationToken);

        IReadOnlyList<OperationClaimResponse> response = claims.Select(c => c.ToResponse()).ToArray();

        return new SuccessDataResult<IReadOnlyList<OperationClaimResponse>>(response);
    }

    [SecuredOperationAspect(Roles.Admin)]
    [ValidationAspect(typeof(SaveOperationClaimRequestValidator))]
    public async Task<IDataResult<OperationClaimResponse>> CreateAsync(
        SaveOperationClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        string name = request.Name.Trim();

        if (await _operationClaimRepository.AnyAsync(c => c.Name == name, cancellationToken))
        {
            throw new ConflictException(Messages.OperationClaimNameInUse);
        }

        var claim = new OperationClaim
        {
            Name = name,
            Description = request.Description?.Trim()
        };

        await _operationClaimRepository.AddAsync(claim, cancellationToken);

        return new SuccessDataResult<OperationClaimResponse>(claim.ToResponse(), Messages.OperationClaimCreated);
    }

    [SecuredOperationAspect(Roles.Admin)]
    [ValidationAspect(typeof(SaveOperationClaimRequestValidator))]
    public async Task<IDataResult<OperationClaimResponse>> UpdateAsync(
        Guid id,
        SaveOperationClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        OperationClaim claim = await _operationClaimRepository.GetAsync(c => c.Id == id, asNoTracking: false,
                                   cancellationToken: cancellationToken)
                               ?? throw new NotFoundException(Messages.OperationClaimNotFound);

        string name = request.Name.Trim();

        if (!string.Equals(claim.Name, name, StringComparison.Ordinal) &&
            await _operationClaimRepository.AnyAsync(c => c.Name == name && c.Id != id, cancellationToken))
        {
            throw new ConflictException(Messages.OperationClaimNameInUse);
        }

        // Sistem yetkilerinin adı değiştirilemez: kod içindeki
        // [Authorize(Roles = "Admin")] kontrolleri ada göre çalışıyor.
        // Adı değiştirmek, o kontrolleri sessizce devre dışı bırakırdı.
        if (IsSystemClaim(claim.Name) && !string.Equals(claim.Name, name, StringComparison.Ordinal))
        {
            throw new BusinessException(
                $"'{claim.Name}' sistem yetkisidir; adı değiştirilemez. Açıklamasını güncelleyebilirsiniz.");
        }

        claim.Name = name;
        claim.Description = request.Description?.Trim();

        await _operationClaimRepository.UpdateAsync(claim, cancellationToken);

        return new SuccessDataResult<OperationClaimResponse>(claim.ToResponse(), Messages.OperationClaimUpdated);
    }

    [SecuredOperationAspect(Roles.Admin)]
    public async Task<IResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        OperationClaim claim = await _operationClaimRepository.GetAsync(c => c.Id == id, asNoTracking: false,
                                   cancellationToken: cancellationToken)
                               ?? throw new NotFoundException(Messages.OperationClaimNotFound);

        if (IsSystemClaim(claim.Name))
        {
            throw new BusinessException($"'{claim.Name}' sistem yetkisidir ve silinemez.");
        }

        await _operationClaimRepository.DeleteAsync(claim, cancellationToken: cancellationToken);

        return new SuccessResult(Messages.OperationClaimDeleted);
    }

    [SecuredOperationAspect(Roles.Admin)]
    [ValidationAspect(typeof(AssignOperationClaimRequestValidator))]
    public async Task<IResult> AssignToUserAsync(
        AssignOperationClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureUserAndClaimExistAsync(request, cancellationToken);

        if (await _userOperationClaimRepository.AnyAsync(
                uoc => uoc.UserId == request.UserId && uoc.OperationClaimId == request.OperationClaimId,
                cancellationToken))
        {
            throw new ConflictException(Messages.ClaimAlreadyAssigned);
        }

        await _userOperationClaimRepository.AddAsync(
            new UserOperationClaim
            {
                UserId = request.UserId,
                OperationClaimId = request.OperationClaimId
            },
            cancellationToken);

        return new SuccessResult(Messages.ClaimAssigned);
    }

    [SecuredOperationAspect(Roles.Admin)]
    [ValidationAspect(typeof(AssignOperationClaimRequestValidator))]
    public async Task<IResult> RevokeFromUserAsync(
        AssignOperationClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        UserOperationClaim assignment = await _userOperationClaimRepository.GetAsync(
                                            uoc => uoc.UserId == request.UserId
                                                   && uoc.OperationClaimId == request.OperationClaimId,
                                            asNoTracking: false,
                                            cancellationToken: cancellationToken)
                                        ?? throw new NotFoundException("Yetki ataması bulunamadı.");

        // Yöneticinin kendi Admin yetkisini kaldırması, sisteme yönetici
        // erişiminin kalıcı olarak kaybedilmesine yol açabilir.
        if (request.UserId == _currentUser.UserId)
        {
            OperationClaim? claim = await _operationClaimRepository.GetAsync(
                c => c.Id == request.OperationClaimId, cancellationToken: cancellationToken);

            if (claim is not null && string.Equals(claim.Name, Roles.Admin, StringComparison.Ordinal))
            {
                throw new BusinessException("Kendi yönetici yetkinizi kaldıramazsınız.");
            }
        }

        await _userOperationClaimRepository.DeleteAsync(assignment, hardDelete: true, cancellationToken);

        return new SuccessResult(Messages.ClaimRevoked);
    }

    private async Task EnsureUserAndClaimExistAsync(
        AssignOperationClaimRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _userRepository.AnyAsync(u => u.Id == request.UserId, cancellationToken))
        {
            throw new NotFoundException(Messages.UserNotFound);
        }

        if (!await _operationClaimRepository.AnyAsync(c => c.Id == request.OperationClaimId, cancellationToken))
        {
            throw new NotFoundException(Messages.OperationClaimNotFound);
        }
    }

    private static bool IsSystemClaim(string name) =>
        name is Roles.Admin or Roles.CategoryManage or Roles.QuestionManage or Roles.UserManage;
}
