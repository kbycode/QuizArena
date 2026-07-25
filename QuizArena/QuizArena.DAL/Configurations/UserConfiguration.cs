using QuizArena.Core.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuizArena.DAL.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.NormalizedEmail).IsRequired().HasMaxLength(256);
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(64);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(64);
        builder.Property(u => u.Nickname).IsRequired().HasMaxLength(32);
        builder.Property(u => u.AvatarUrl).HasMaxLength(512);
        builder.Property(u => u.City).HasMaxLength(64);

        // PBKDF2 kodlu özet: "pbkdf2-sha256$600000$<24 kar. tuz>$<44 kar. özet>"
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
        builder.Property(u => u.SecurityStamp).IsRequired().HasMaxLength(64);

        // Aynı e-posta ile ikinci kayıt açılmasını UYGULAMA DEĞİL VERİTABANI
        // engeller. Uygulamadaki "var mı?" kontrolü ile "ekle" arasında geçen
        // mikrosaniyelerde ikinci bir istek gelirse (race condition) kontrol
        // atlatılabilir; tekil indeks atlatılamaz.
        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("UX_Users_NormalizedEmail");

        // Takma ad sıralama tablosunda görünür; benzersiz olması taklit
        // (impersonation) girişimlerini engeller.
        builder.HasIndex(u => u.Nickname)
            .IsUnique()
            .HasDatabaseName("UX_Users_Nickname");

        builder.HasMany(u => u.UserOperationClaims)
            .WithOne(uoc => uoc.User)
            .HasForeignKey(uoc => uoc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Hesaplanan özellik veritabanına yazılmaz.
        builder.Ignore(u => u.FullName);
    }
}
