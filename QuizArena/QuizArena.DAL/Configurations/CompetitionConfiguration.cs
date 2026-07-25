using QuizArena.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuizArena.DAL.Configurations;

public sealed class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    public void Configure(EntityTypeBuilder<Competition> builder)
    {
        builder.ToTable("Competitions");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Status).HasConversion<int>();

        // Bir katılımcının odada tek bir yarışma oturumu olur. Tekil kısıt,
        // "başlat" isteğinin iki kez gönderilmesiyle aynı oyuncuya iki soru
        // seti açılmasını engeller.
        builder.HasIndex(c => c.RoomParticipantId)
            .IsUnique()
            .HasDatabaseName("UX_Competitions_RoomParticipant");

        // Skor tablosu sorgusu: odadaki yarışmaları puana göre sırala.
        builder.HasIndex(c => new { c.RoomId, c.TotalScore })
            .HasDatabaseName("IX_Competitions_Room_Score");

        builder.HasMany(c => c.CompetitionQuestions)
            .WithOne(cq => cq.Competition)
            .HasForeignKey(cq => cq.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(c => c.AnsweredCount);
        builder.Ignore(c => c.AccuracyPercentage);
    }
}
