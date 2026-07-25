using QuizArena.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuizArena.DAL.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Text).IsRequired().HasMaxLength(512);
        builder.Property(q => q.Explanation).HasMaxLength(1024);

        // Enum veritabanında int olarak saklanır (varsayılan davranış).
        // String saklamak okunaklı olurdu ama indeks/karşılaştırma maliyeti
        // ve yeniden adlandırmada kırılganlık getirir.
        builder.Property(q => q.Difficulty).HasConversion<int>();

        // Yarışma kurulurken çalışan ana sorgu:
        // "bu kategoride aktif sorulardan N tane rastgele seç".
        // Bileşik indeks bu sorguyu tablo taramasından kurtarır.
        builder.HasIndex(q => new { q.CategoryId, q.IsActive })
            .HasDatabaseName("IX_Questions_Category_IsActive");

        builder.HasMany(q => q.Answers)
            .WithOne(a => a.Question)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
