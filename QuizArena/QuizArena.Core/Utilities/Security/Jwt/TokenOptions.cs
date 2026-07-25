using System.ComponentModel.DataAnnotations;

namespace QuizArena.Core.Utilities.Security.Jwt;

/// <summary>
/// JWT ayarları. <c>appsettings</c> içindeki <c>TokenOptions</c> bölümünden bağlanır.
/// </summary>
/// <remarks>
/// Alanlardaki <c>DataAnnotations</c> nitelikleri süs değil:
/// <c>ValidateOnStart()</c> ile birlikte uygulama, ayarlar eksik/zayıfsa
/// <b>ilk istek gelmeden</b> açılışta patlar. "Secret unutulmuş, üretimde
/// imzasız token üretiliyor" sınıfı hataları imkânsız hâle getirir.
/// </remarks>
public sealed class TokenOptions
{
    public const string SectionName = "TokenOptions";

    [Required(ErrorMessage = "TokenOptions:Audience zorunludur.")]
    public string Audience { get; set; } = null!;

    [Required(ErrorMessage = "TokenOptions:Issuer zorunludur.")]
    public string Issuer { get; set; } = null!;

    /// <summary>
    /// Erişim jetonu ömrü (dakika). Kısa tutulur; uzun oturum ihtiyacını
    /// refresh token karşılar. Böylece bir jeton sızsa bile kullanım penceresi
    /// dakikalarla sınırlı kalır.
    /// </summary>
    [Range(1, 240, ErrorMessage = "TokenOptions:AccessTokenExpirationMinutes 1-240 arasında olmalıdır.")]
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    [Range(1, 90, ErrorMessage = "TokenOptions:RefreshTokenExpirationDays 1-90 arasında olmalıdır.")]
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// HMAC-SHA256 imza anahtarı.
    /// <para>
    /// <b>Bu değer kaynak koda yazılmaz.</b> Geliştirmede
    /// <c>dotnet user-secrets</c>, üretimde ortam değişkeni
    /// (<c>TokenOptions__SecurityKey</c>) veya bir secret yöneticisi ile verilir.
    /// En az 32 bayt olmalıdır: HMAC-SHA256'nın blok boyutundan kısa anahtar
    /// imzanın etkin gücünü düşürür.
    /// </para>
    /// </summary>
    [Required(ErrorMessage = "TokenOptions:SecurityKey zorunludur. 'dotnet user-secrets set \"TokenOptions:SecurityKey\" \"...\"' ile verin.")]
    [MinLength(32, ErrorMessage = "TokenOptions:SecurityKey en az 32 karakter olmalıdır (HMAC-SHA256).")]
    public string SecurityKey { get; set; } = null!;
}
