using QuizArena.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuizArena.DAL.Configurations;

public sealed class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement>
{
    public void Configure(EntityTypeBuilder<UserAchievement> builder)
    {
        builder.ToTable("UserAchievements");
        builder.HasKey(ua => ua.Id);

        // Aynı rozet iki kez verilemez — ödül puanının tekrar tekrar
        // kazanılmasını engeller.
        builder.HasIndex(ua => new { ua.UserId, ua.AchievementId })
            .IsUnique()
            .HasDatabaseName("UX_UserAchievements_User_Achievement");

        builder.HasOne(ua => ua.User)
            .WithMany()
            .HasForeignKey(ua => ua.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
