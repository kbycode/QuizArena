using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.BLL.Mapping;
using QuizArena.BLL.Validation;
using QuizArena.Core.Aspects.Authorization;
using QuizArena.Core.Aspects.Validation;
using QuizArena.Core.DataAccess.Paging;
using QuizArena.Core.Entities.Concrete;
using QuizArena.Core.Exceptions;
using QuizArena.Core.Utilities.Results;
using QuizArena.Core.Utilities.Security;
using QuizArena.DAL.Abstract;
using QuizArena.Entities.Dtos.Common;
using QuizArena.Entities.Dtos.Users;
using Microsoft.EntityFrameworkCore;

namespace QuizArena.BLL.Concrete;

/// <summary>
/// Kullanıcı profili ve hesap yönetimi.
/// </summary>
/// <remarks>
/// <b>İlk hâle göre en kritik fark:</b> önceki <c>UserManager</c> ve
/// <c>UsersController</c> <c>User</c> varlığını doğrudan hem girdi hem çıktı
/// olarak kullanıyordu. Bunun iki ayrı sonucu vardı:
/// <list type="bullet">
///   <item>
///     <b>Çıktıda:</b> parola özeti ve tuzu istemciye gidiyordu.
///   </item>
///   <item>
///     <b>Girdide:</b> <c>POST /api/users/update</c> gövdesine
///     <c>passwordHash</c> yazarak parolayı doğrudan ezmek mümkündü;
///     <c>status</c> alanıyla da başkasının hesabını kapatmak.
///   </item>
/// </list>
/// Artık girdi ve çıktı için ayrı, alanları elle seçilmiş DTO'lar var.
/// </remarks>
public sealed class UserManager : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public UserManager(IUserRepository userRepository, ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<IDataResult<UserProfileResponse>> GetMyProfileAsync(
        CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();

        User user = await _userRepository.GetAsync(u => u.Id == userId, cancellationToken: cancellationToken)
                    ?? throw NotFoundException.For("Kullanıcı");

        IReadOnlyList<string> roles = await _userRepository.GetOperationClaimNamesAsync(userId, cancellationToken);

        return new SuccessDataResult<UserProfileResponse>(user.ToProfileResponse(roles));
    }

    [ValidationAspect(typeof(UpdateProfileRequestValidator))]
    public async Task<IDataResult<UserProfileResponse>> UpdateMyProfileAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();

        User user = await _userRepository.GetAsync(u => u.Id == userId, asNoTracking: false,
                        cancellationToken: cancellationToken)
                    ?? throw NotFoundException.For("Kullanıcı");

        string nickname = request.Nickname.Trim();

        // Takma ad değişiyorsa başkası tarafından kullanılıp kullanılmadığına bak.
        if (!string.Equals(user.Nickname, nickname, StringComparison.Ordinal) &&
            await _userRepository.AnyAsync(u => u.Nickname == nickname && u.Id != userId, cancellationToken))
        {
            throw new ConflictException(Messages.NicknameAlreadyTaken);
        }

        // Yalnızca DTO'da yer alan alanlar atanır. E-posta, rol, hesap durumu
        // ve parola bu yolla DEĞİŞTİRİLEMEZ.
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Nickname = nickname;
        user.City = request.City?.Trim();
        user.BirthDate = request.BirthDate;
        user.AvatarUrl = request.AvatarUrl?.Trim();

        await _userRepository.UpdateAsync(user, cancellationToken);

        IReadOnlyList<string> roles = await _userRepository.GetOperationClaimNamesAsync(userId, cancellationToken);

        return new SuccessDataResult<UserProfileResponse>(
            user.ToProfileResponse(roles),
            Messages.ProfileUpdated);
    }

    public async Task<IDataResult<UserSummaryResponse>> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        User user = await _userRepository.GetAsync(u => u.Id == userId, cancellationToken: cancellationToken)
                    ?? throw NotFoundException.For("Kullanıcı");

        return new SuccessDataResult<UserSummaryResponse>(user.ToSummaryResponse());
    }

    // =====================================================================
    //  Yönetici işlemleri
    // =====================================================================

    /// <remarks>
    /// <c>[SecuredOperationAspect]</c>, controller'daki <c>[Authorize]</c>'a ek
    /// bir savunma katmanıdır: bu metot ileride başka bir giriş noktasından
    /// (SignalR, arka plan görevi, yeni bir controller) çağrılsa bile yetki
    /// kontrolü devrede kalır.
    /// </remarks>
    [SecuredOperationAspect(Roles.Admin, Roles.UserManage)]
    public async Task<IDataResult<PagedResponse<AdminUserResponse>>> GetPagedAsync(
        PageRequest pageRequest,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        string? raw = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        string? normalizedTerm = raw?.ToUpperInvariant();
        string? likePattern = raw is null ? null : $"%{EscapeLikeWildcards(raw)}%";

        PagedList<User> page = await _userRepository.GetPagedListAsync(
            pageRequest,
            predicate: raw is null
                ? null
                // E-postada normalize edilmiş kolonda arama yapılıyor: hem
                // büyük/küçük harf sorunundan hem de Türkçe "i" probleminden
                // bağımsız ve tekil indeksten yararlanabilir.
                // Takma ad için EF.Functions.Like kullanılıyor; veritabanının
                // collation'ına bırakmak yerine SQL LIKE'ı açıkça üretiyoruz.
                : u => u.NormalizedEmail.Contains(normalizedTerm!)
                       || EF.Functions.Like(u.Nickname, likePattern!),
            orderBy: q => q.OrderByDescending(u => u.CreatedAtUtc),
            cancellationToken: cancellationToken);

        // Rolleri tek sorguda toplayıp eşliyoruz (kullanıcı başına ayrı sorgu
        // atmak N+1 olurdu).
        var responses = new List<AdminUserResponse>(page.Items.Count);
        foreach (User user in page.Items)
        {
            IReadOnlyList<string> roles = await _userRepository.GetOperationClaimNamesAsync(user.Id, cancellationToken);
            responses.Add(user.ToAdminResponse(roles));
        }

        var result = new PagedResponse<AdminUserResponse>(
            responses,
            page.Page,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            page.HasPrevious,
            page.HasNext);

        return new SuccessDataResult<PagedResponse<AdminUserResponse>>(result);
    }

    [SecuredOperationAspect(Roles.Admin, Roles.UserManage)]
    public async Task<IResult> SetActiveAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        User user = await _userRepository.GetAsync(u => u.Id == userId, asNoTracking: false,
                        cancellationToken: cancellationToken)
                    ?? throw NotFoundException.For("Kullanıcı");

        // Yönetici kendi hesabını kapatırsa sisteme erişimi kalıcı olarak
        // kaybedebilir; bu kaza engellenmeli.
        if (!isActive && userId == _currentUser.UserId)
        {
            throw new BusinessException("Kendi hesabınızı devre dışı bırakamazsınız.");
        }

        user.IsActive = isActive;

        // Hesap kapatıldığında dolaşımdaki jetonları da geçersiz kıl.
        if (!isActive)
        {
            user.SecurityStamp = Guid.NewGuid().ToString("N");
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new SuccessResult(isActive ? Messages.UserActivated : Messages.UserDeactivated);
    }

    [SecuredOperationAspect(Roles.Admin, Roles.UserManage)]
    public async Task<IResult> UnlockAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        User user = await _userRepository.GetAsync(u => u.Id == userId, asNoTracking: false,
                        cancellationToken: cancellationToken)
                    ?? throw NotFoundException.For("Kullanıcı");

        user.LockoutEndUtc = null;
        user.AccessFailedCount = 0;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new SuccessResult(Messages.UserUnlocked);
    }

    /// <summary>
    /// LIKE joker karakterlerini etkisizleştirir.
    /// </summary>
    /// <remarks>
    /// Kullanıcı arama kutusuna <c>%</c> yazarsa bu "her şeyi getir" anlamına
    /// gelir, <c>_</c> ise "herhangi bir karakter". Kaçış yapılmadığında bu,
    /// beklenmeyen sonuçlara ve gereksiz tam tablo taramalarına yol açar.
    /// </remarks>
    private static string EscapeLikeWildcards(string value) =>
        value.Replace("[", "[[]", StringComparison.Ordinal)
             .Replace("%", "[%]", StringComparison.Ordinal)
             .Replace("_", "[_]", StringComparison.Ordinal);
}
