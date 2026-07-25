using QuizArena.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuizArena.DAL.Configurations;

public sealed class CompetitionAnswerConfiguration : IEntityTypeConfiguration<CompetitionAnswer>
{
    public void Configure(EntityTypeBuilder<CompetitionAnswer> builder)
    {
        builder.ToTable("CompetitionAnswers");
        builder.HasKey(ca => ca.Id);

        // Not: CompetitionQuestionId üzerindeki TEKİL indeks, bire-bir ilişki
        // tanımı (CompetitionQuestionConfiguration) tarafından zaten üretiliyor.
        // Bu kısıt, "aynı soruya ikinci cevap gönderip puan katlama"
        // saldırısına karşı son savunma hattıdır: iki istek aynı anda gelse
        // bile ikincisi veritabanı seviyesinde reddedilir.

        builder.HasOne(ca => ca.SelectedAnswer)
            .WithMany()
            .HasForeignKey(ca => ca.SelectedAnswerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
