using QuizArena.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuizArena.DAL.Configurations;

public sealed class UserStatisticConfiguration : IEntityTypeConfiguration<UserStatistic>
{
    public void Configure(EntityTypeBuilder<UserStatistic> builder)
    {
        builder.ToTable("UserStatistics");
        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.UserId)
            .IsUnique()
            .HasDatabaseName("UX_UserStatistics_User");

        // Sıralama tablosunun ana sorgusu: puana göre azalan ilk N kayıt.
        // Azalan indeks sayesinde sıralama işlemi tamamen indeksten karşılanır.
        builder.HasIndex(s => s.TotalScore)
            .IsDescending()
            .HasDatabaseName("IX_UserStatistics_TotalScore_Desc");

        builder.HasOne(s => s.User)
            .WithOne()
            .HasForeignKey<UserStatistic>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(s => s.AccuracyPercentage);
    }
}
