using QuizArena.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuizArena.DAL.Configurations;

public sealed class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.ToTable("Achievements");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Code).HasConversion<int>();
        builder.Property(a => a.Name).IsRequired().HasMaxLength(64);
        builder.Property(a => a.Description).IsRequired().HasMaxLength(256);
        builder.Property(a => a.Icon).IsRequired().HasMaxLength(16);

        builder.HasIndex(a => a.Code)
            .IsUnique()
            .HasDatabaseName("UX_Achievements_Code");

        builder.HasMany(a => a.UserAchievements)
            .WithOne(ua => ua.Achievement)
            .HasForeignKey(ua => ua.AchievementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
