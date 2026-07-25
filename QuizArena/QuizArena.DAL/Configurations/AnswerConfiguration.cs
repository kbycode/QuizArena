using QuizArena.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuizArena.DAL.Configurations;

public sealed class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.ToTable("Answers");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Text).IsRequired().HasMaxLength(256);

        builder.HasIndex(a => new { a.QuestionId, a.DisplayOrder })
            .HasDatabaseName("IX_Answers_Question_Order");
    }
}
