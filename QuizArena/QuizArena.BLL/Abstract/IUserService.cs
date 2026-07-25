using QuizArena.Core.DataAccess.Paging;
using QuizArena.Core.Utilities.Results;
using QuizArena.Entities.Dtos.Common;
using QuizArena.Entities.Dtos.Users;

namespace QuizArena.BLL.Abstract;

/// <summary>Kullanıcı profili ve (yönetici için) hesap yönetimi.</summary>
public interface IUserService
{
    /// <summary>Oturum açmış kullanıcının kendi profili.</summary>
    Task<IDataResult<UserProfileResponse>> GetMyProfileAsync(CancellationToken cancellationToken = default);

    Task<IDataResult<UserProfileResponse>> UpdateMyProfileAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Başka bir kullanıcının herkese açık görünümü.</summary>
    Task<IDataResult<UserSummaryResponse>> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    // --- Yönetici işlemleri --------------------------------------------------
    Task<IDataResult<PagedResponse<AdminUserResponse>>> GetPagedAsync(
        PageRequest pageRequest,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<IResult> SetActiveAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default);

    /// <summary>Kaba kuvvet koruması nedeniyle kilitlenmiş hesabı açar.</summary>
    Task<IResult> UnlockAsync(Guid userId, CancellationToken cancellationToken = default);
}
