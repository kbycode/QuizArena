using QuizArena.Core.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuizArena.DAL.Configurations;

public sealed class OperationClaimConfiguration : IEntityTypeConfiguration<OperationClaim>
{
    public void Configure(EntityTypeBuilder<OperationClaim> builder)
    {
        builder.ToTable("OperationClaims");
        builder.HasKey(oc => oc.Id);

        builder.Property(oc => oc.Name).IsRequired().HasMaxLength(64);
        builder.Property(oc => oc.Description).HasMaxLength(256);

        builder.HasIndex(oc => oc.Name)
            .IsUnique()
            .HasDatabaseName("UX_OperationClaims_Name");
    }
}
