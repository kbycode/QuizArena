using QuizArena.Core.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuizArena.DAL.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(rt => rt.Id);

        // SHA-256 → 32 bayt → Base64'te 44 karakter.
        builder.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(64);
        builder.Property(rt => rt.ReplacedByTokenHash).HasMaxLength(64);
        builder.Property(rt => rt.CreatedByIp).HasMaxLength(64);   // IPv6 + port
        builder.Property(rt => rt.RevokedByIp).HasMaxLength(64);
        builder.Property(rt => rt.RevokeReason).HasMaxLength(128);

        // Yenileme isteği geldiğinde jeton özetiyle arama yapılır; tekil
        // indeks hem aramayı indeksli hâle getirir hem de aynı özetin
        // iki kez kaydedilmesini imkânsız kılar.
        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique()
            .HasDatabaseName("UX_RefreshTokens_TokenHash");

        // "Bu kullanıcının aktif jetonları" sorgusu için.
        builder.HasIndex(rt => new { rt.UserId, rt.ExpiresAtUtc })
            .HasDatabaseName("IX_RefreshTokens_User_Expiry");
    }
}
