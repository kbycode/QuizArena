using QuizArena.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuizArena.DAL.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(64);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(80);
        builder.Property(c => c.Description).HasMaxLength(512);
        builder.Property(c => c.Icon).HasMaxLength(16);
        builder.Property(c => c.ColorHex).HasMaxLength(9);   // #RRGGBBAA

        builder.HasIndex(c => c.Slug)
            .IsUnique()
            .HasDatabaseName("UX_Categories_Slug");

        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("UX_Categories_Name");

        builder.HasMany(c => c.Questions)
            .WithOne(q => q.Category)
            .HasForeignKey(q => q.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Kategori silinince odalar silinmez: geçmiş yarışma sonuçları
        // korunmalı. (Kategoriler zaten yumuşak silinir.)
        builder.HasMany(c => c.Rooms)
            .WithOne(r => r.Category)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
