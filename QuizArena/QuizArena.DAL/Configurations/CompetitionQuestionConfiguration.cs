using QuizArena.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuizArena.DAL.Configurations;

public sealed class CompetitionQuestionConfiguration : IEntityTypeConfiguration<CompetitionQuestion>
{
    public void Configure(EntityTypeBuilder<CompetitionQuestion> builder)
    {
        builder.ToTable("CompetitionQuestions");
        builder.HasKey(cq => cq.Id);

        // Bir yarışmada aynı sıra numarası iki kez olamaz.
        builder.HasIndex(cq => new { cq.CompetitionId, cq.Order })
            .IsUnique()
            .HasDatabaseName("UX_CompetitionQuestions_Competition_Order");

        // Aynı soru bir yarışmada iki kez sorulamaz — "aynı soru üç kez geldi"
        // şikâyetini kod değil veri modeli engeller.
        builder.HasIndex(cq => new { cq.CompetitionId, cq.QuestionId })
            .IsUnique()
            .HasDatabaseName("UX_CompetitionQuestions_Competition_Question");

        builder.HasOne(cq => cq.Question)
            .WithMany()
            .HasForeignKey(cq => cq.QuestionId)
            // Soru silinse bile geçmiş yarışma kayıtları bozulmaz.
            .OnDelete(DeleteBehavior.Restrict);

        // Bire-bir: her yarışma sorusuna en fazla bir cevap.
        builder.HasOne(cq => cq.Answer)
            .WithOne(ca => ca.CompetitionQuestion)
            .HasForeignKey<CompetitionAnswer>(ca => ca.CompetitionQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(cq => cq.IsAsked);
    }
}
