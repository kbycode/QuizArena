using QuizArena.BLL.Abstract;
using QuizArena.BLL.Constants;
using QuizArena.BLL.Mapping;
using QuizArena.BLL.Validation;
using QuizArena.Core.Aspects.Transaction;
using QuizArena.Core.Aspects.Validation;
using QuizArena.Core.Entities.Concrete;
using QuizArena.Core.Exceptions;
using QuizArena.Core.Utilities.Clock;
using QuizArena.Core.Utilities.Results;
using QuizArena.Core.Utilities.Security;
using QuizArena.Core.Utilities.Security.Hashing;
using QuizArena.Core.Utilities.Security.Jwt;
using QuizArena.DAL.Abstract;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Dtos.Auth;
using Microsoft.Extensions.Logging;

namespace QuizArena.BLL.Concrete;

/// <summary>
/// Kimlik doğrulama iş kuralları.
/// </summary>
/// <remarks>
/// <para>
/// Kimlik doğrulamada uygulanan beş kural — her biri somut bir saldırıya
/// veya arıza biçimine karşı:
/// </para>
/// <list type="number">
///   <item>
///     <b>Parola alanları <c>null</c> olabilir kabul edilir.</b> Parola özeti
///     taşımayan bir kayıt, doğrulamada sessizce reddedilir; aksi hâlde
///     böyle bir kullanıcı girişte <c>NullReferenceException</c> ile 500
///     üretirdi.
///   </item>
///   <item>
///     <b>Kullanıcı numaralandırmaya kapalı.</b> "Kullanıcı bulunamadı" ve
///     "parola eşleşmiyor" <b>aynı</b> mesajı döner. Farklı olsalardı
///     saldırgan hangi e-postaların kayıtlı olduğunu tek tek öğrenebilirdi.
///   </item>
///   <item>
///     <b>Kaba kuvvet sayılır.</b> Başarısız denemeler kaydedilir ve hesap
///     geçici olarak kilitlenir.
///   </item>
///   <item>
///     <b>E-posta tekilliği servisin içinde kontrol edilir</b>, çağıranda
///     değil. "Önce kontrol et, sonra kaydet" iki ayrı çağrıya bölündüğünde
///     araya giren ikinci bir istek kontrolü atlatır; son güvence yine de
///     veritabanındaki tekil indekstedir.
///   </item>
///   <item>
///     <b>Oturum süresizce açık kalmaz.</b> Kısa ömürlü erişim jetonu ve
///     rotasyonlu yenileme jetonu kullanılır.
///   </item>
/// </list>
/// </remarks>
public sealed class AuthManager : IAuthService
{
    /// <summary>Kaç başarısız denemeden sonra hesap kilitlenir.</summary>
    private const int MaxAccessFailedCount = 5;

    /// <summary>Kilit süresi.</summary>
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserStatisticRepository _userStatisticRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly ILogger<AuthManager> _logger;

    public AuthManager(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUserStatisticRepository userStatisticRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ICurrentUserService currentUser,
        IClock clock,
        ILogger<AuthManager> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _userStatisticRepository = userStatisticRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    // =====================================================================
    //  Kayıt
    // =====================================================================
    [ValidationAspect(typeof(RegisterRequestValidator))]
    [TransactionAspect]
    public async Task<IDataResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        string email = request.Email.Trim();
        string normalizedEmail = Normalize(email);
        string nickname = request.Nickname.Trim();

        if (await _userRepository.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            throw new ConflictException(Messages.EmailAlreadyRegistered);
        }

        if (await _userRepository.AnyAsync(u => u.Nickname == nickname, cancellationToken))
        {
            throw new ConflictException(Messages.NicknameAlreadyTaken);
        }

        var user = new User
        {
            Email = email,
            NormalizedEmail = normalizedEmail,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Nickname = nickname,
            PasswordHash = _passwordHasher.Hash(request.Password),
            SecurityStamp = NewSecurityStamp(),
            IsActive = true,
            LastLoginAtUtc = _clock.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);

        // İstatistik satırı kayıtla birlikte oluşturulur; böylece oyun akışı
        // "istatistik var mı?" kontrolü yapmak zorunda kalmaz.
        await _userStatisticRepository.AddAsync(new UserStatistic { UserId = user.Id }, cancellationToken);

        AuthResponse response = await IssueTokensAsync(user, roles: [], cancellationToken);

        _logger.LogInformation("Yeni kullanıcı kaydı oluşturuldu: {UserId}", user.Id);

        return new SuccessDataResult<AuthResponse>(response, Messages.UserRegistered);
    }

    // =====================================================================
    //  Giriş
    // =====================================================================
    [ValidationAspect(typeof(LoginRequestValidator))]
    public async Task<IDataResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        string normalizedEmail = Normalize(request.Email);

        // Güncelleme yapacağımız için izlemeli çekiyoruz (asNoTracking: false).
        User? user = await _userRepository.GetByNormalizedEmailAsync(
            normalizedEmail, asNoTracking: false, cancellationToken);

        if (user is null)
        {
            // Kullanıcı yoksa da parola doğrulama maliyetini ödüyoruz.
            // Aksi hâlde "var olmayan kullanıcı" isteği belirgin şekilde daha
            // hızlı yanıtlanır ve saldırgan yanıt süresinden hesabın varlığını
            // anlayabilir (timing-based user enumeration).
            _passwordHasher.Verify(request.Password, TimingEqualizerHash);
            throw new UnauthorizedException(Messages.InvalidCredentials);
        }

        if (IsLockedOut(user))
        {
            throw new UnauthorizedException(Messages.AccountLocked);
        }

        PasswordVerificationResult verification = _passwordHasher.Verify(request.Password, user.PasswordHash);

        if (verification == PasswordVerificationResult.Failed)
        {
            await RegisterFailedAttemptAsync(user, cancellationToken);
            throw new UnauthorizedException(Messages.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            // Sıra önemli: parola doğrulanmadan "hesap kapalı" demek, geçerli
            // bir e-postanın sistemde var olduğunu parolayı bilmeyen birine
            // söylemek olurdu.
            throw new UnauthorizedException(Messages.AccountInactive);
        }

        // Parola doğru: sayaç sıfırlanır, kilit kalkar.
        user.AccessFailedCount = 0;
        user.LockoutEndUtc = null;
        user.LastLoginAtUtc = _clock.UtcNow;

        // Özet eski parametrelerle üretilmişse sessizce güncelle. Kullanıcı
        // hiçbir şey fark etmez; parola elimizde olduğu tek an burasıdır.
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.Hash(request.Password);
            _logger.LogInformation("Kullanıcı parola özeti güncel parametrelerle yenilendi: {UserId}", user.Id);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        IReadOnlyList<string> roles = await _userRepository.GetOperationClaimNamesAsync(user.Id, cancellationToken);
        AuthResponse response = await IssueTokensAsync(user, roles, cancellationToken);

        return new SuccessDataResult<AuthResponse>(response, Messages.LoginSuccessful);
    }

    // =====================================================================
    //  Jeton yenileme (rotasyon + yeniden kullanım tespiti)
    // =====================================================================
    [ValidationAspect(typeof(RefreshTokenRequestValidator))]
    [TransactionAspect]
    public async Task<IDataResult<AuthResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        string tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        DateTime now = _clock.UtcNow;

        RefreshToken? stored = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (stored is null)
        {
            throw new UnauthorizedException(Messages.InvalidRefreshToken);
        }

        // --- Jeton yeniden kullanımı (token replay) tespiti -----------------
        // İptal edilmiş bir jeton yeniden kullanılıyorsa iki olasılık var:
        // ya jeton çalındı ya da meşru kullanıcının eski jetonu bir yerde
        // kaldı. İkisini ayırt edemediğimiz için en güvenli yolu seçiyoruz:
        // kullanıcının TÜM oturumlarını düşürüyoruz.
        if (stored.IsRevoked)
        {
            await _refreshTokenRepository.RevokeAllActiveAsync(
                stored.UserId,
                "Iptal edilmis jeton yeniden kullanildi (olasi hirsizlik).",
                _currentUser.IpAddress,
                now,
                cancellationToken);

            _logger.LogWarning(
                "İptal edilmiş yenileme jetonu tekrar kullanıldı. Kullanıcının tüm oturumları kapatıldı: {UserId}",
                stored.UserId);

            throw new UnauthorizedException(Messages.InvalidRefreshToken);
        }

        if (stored.IsExpired(now))
        {
            throw new UnauthorizedException(Messages.InvalidRefreshToken);
        }

        User user = stored.User;

        if (!user.IsActive)
        {
            throw new UnauthorizedException(Messages.AccountInactive);
        }

        IReadOnlyList<string> roles = await _userRepository.GetOperationClaimNamesAsync(user.Id, cancellationToken);
        AccessToken accessToken = _tokenService.CreateAccessToken(user, roles);
        string newTokenHash = _tokenService.HashRefreshToken(accessToken.RefreshToken);

        // Rotasyon: eski jeton iptal edilir ve yerini alan jetona bağlanır.
        stored.RevokedAtUtc = now;
        stored.RevokedByIp = _currentUser.IpAddress;
        stored.RevokeReason = "Rotasyon";
        stored.ReplacedByTokenHash = newTokenHash;
        await _refreshTokenRepository.UpdateAsync(stored, cancellationToken);

        await _refreshTokenRepository.AddAsync(
            new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newTokenHash,
                ExpiresAtUtc = accessToken.RefreshTokenExpiresAtUtc,
                CreatedByIp = _currentUser.IpAddress
            },
            cancellationToken);

        var response = new AuthResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            accessToken.RefreshToken,
            accessToken.RefreshTokenExpiresAtUtc,
            user.ToProfileResponse(roles));

        return new SuccessDataResult<AuthResponse>(response, Messages.TokenRefreshed);
    }

    // =====================================================================
    //  Çıkış
    // =====================================================================
    [ValidationAspect(typeof(RefreshTokenRequestValidator))]
    public async Task<IResult> LogoutAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        string tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        RefreshToken? stored = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        // Jeton bulunamasa bile başarı dönülür: "bu jeton var mıydı?" bilgisi
        // istemciye verilmemeli ve çıkış işlemi hiçbir zaman hata üretmemeli
        // (idempotent davranış).
        if (stored is not null && !stored.IsRevoked)
        {
            stored.RevokedAtUtc = _clock.UtcNow;
            stored.RevokedByIp = _currentUser.IpAddress;
            stored.RevokeReason = "Kullanici cikis yapti";
            await _refreshTokenRepository.UpdateAsync(stored, cancellationToken);
        }

        return new SuccessResult(Messages.LoggedOut);
    }

    // =====================================================================
    //  Parola değiştirme
    // =====================================================================
    [ValidationAspect(typeof(ChangePasswordRequestValidator))]
    [TransactionAspect]
    public async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.RequireUserId();

        User user = await _userRepository.GetAsync(u => u.Id == userId, asNoTracking: false,
                       cancellationToken: cancellationToken)
                   ?? throw NotFoundException.For("Kullanıcı");

        if (_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash) == PasswordVerificationResult.Failed)
        {
            throw new BusinessException(Messages.CurrentPasswordIncorrect);
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);

        // Güvenlik damgası yenilenir: elde dolaşan erişim jetonları,
        // ömürleri dolmasa bile geçersiz hâle gelir.
        user.SecurityStamp = NewSecurityStamp();

        await _userRepository.UpdateAsync(user, cancellationToken);

        // Parola değişiminde tüm yenileme jetonları düşer. Hesabı ele geçirmiş
        // biri varsa erişimi burada kesilir — parolayı değiştirmenin asıl amacı.
        await _refreshTokenRepository.RevokeAllActiveAsync(
            user.Id,
            "Parola degistirildi",
            _currentUser.IpAddress,
            _clock.UtcNow,
            cancellationToken);

        _logger.LogInformation("Kullanıcı parolasını değiştirdi: {UserId}", user.Id);

        return new SuccessResult(Messages.PasswordChanged);
    }

    // =====================================================================
    //  Yardımcılar
    // =====================================================================

    /// <summary>
    /// Var olmayan kullanıcı için doğrulama maliyetini taklit etmek üzere
    /// kullanılan, geçerli biçimli ama hiçbir parolayla eşleşmeyen özet.
    /// </summary>
    /// <remarks>
    /// Özet <b>enjekte edilen</b> hasher ile üretiliyor; böylece iterasyon
    /// sayısı gerçek doğrulamayla birebir aynı olur ve süre farkı kapanır.
    /// Sabit yazılmış düşük iterasyonlu bir özet kullanılsa, taklit doğrulama
    /// gerçeğinden çok daha hızlı biterdi ve önlem işlevsiz kalırdı.
    /// <para>
    /// İlk hatalı denemede bir kez hesaplanır ve saklanır. Referans ataması
    /// atomik olduğu için eşzamanlı istekler en kötü hâlde değeri iki kez
    /// üretir — zararsız.
    /// </para>
    /// </remarks>
    private static string? _timingEqualizerHash;

    private string TimingEqualizerHash =>
        _timingEqualizerHash ??= _passwordHasher.Hash("zaman-esitleme-icin-uretilmis-ozet");

    private async Task<AuthResponse> IssueTokensAsync(
        User user,
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken)
    {
        AccessToken accessToken = _tokenService.CreateAccessToken(user, roles);

        await _refreshTokenRepository.AddAsync(
            new RefreshToken
            {
                UserId = user.Id,
                TokenHash = _tokenService.HashRefreshToken(accessToken.RefreshToken),
                ExpiresAtUtc = accessToken.RefreshTokenExpiresAtUtc,
                CreatedByIp = _currentUser.IpAddress
            },
            cancellationToken);

        return new AuthResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            accessToken.RefreshToken,
            accessToken.RefreshTokenExpiresAtUtc,
            user.ToProfileResponse(roles));
    }

    private bool IsLockedOut(User user) =>
        user.LockoutEndUtc is not null && user.LockoutEndUtc > _clock.UtcNow;

    private async Task RegisterFailedAttemptAsync(User user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;

        if (user.AccessFailedCount >= MaxAccessFailedCount)
        {
            user.LockoutEndUtc = _clock.UtcNow.Add(LockoutDuration);
            user.AccessFailedCount = 0;

            _logger.LogWarning(
                "Hesap {LockoutMinutes} dakika kilitlendi (çok sayıda hatalı giriş): {UserId}",
                LockoutDuration.TotalMinutes,
                user.Id);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    /// <summary>
    /// E-postayı kültürden bağımsız olarak normalize eder.
    /// </summary>
    /// <remarks>
    /// <c>ToUpper()</c> yerine <c>ToUpperInvariant()</c> kullanılması Türkçe
    /// için <b>zorunludur</b>: Türkçe kültürde <c>"i".ToUpper()</c> sonucu
    /// <c>"İ"</c>'dir. Sunucunun kültürü değiştiğinde aynı e-posta farklı
    /// normalize edilir ve kullanıcı kendi hesabına giriş yapamaz hâle gelir
    /// (klasik "Turkish i problem").
    /// </remarks>
    private static string Normalize(string email) => email.Trim().ToUpperInvariant();

    private static string NewSecurityStamp() => Guid.NewGuid().ToString("N");
}
