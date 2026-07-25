using QuizArena.Core.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuizArena.DAL.Configurations;

public sealed class UserOperationClaimConfiguration : IEntityTypeConfiguration<UserOperationClaim>
{
    public void Configure(EntityTypeBuilder<UserOperationClaim> builder)
    {
        builder.ToTable("UserOperationClaims");
        builder.HasKey(uoc => uoc.Id);

        // Aynı yetkinin aynı kullanıcıya iki kez atanması anlamsız; ayrıca
        // JWT'ye aynı rolün iki kez yazılmasına yol açardı.
        builder.HasIndex(uoc => new { uoc.UserId, uoc.OperationClaimId })
            .IsUnique()
            .HasDatabaseName("UX_UserOperationClaims_User_Claim");

        builder.HasOne(uoc => uoc.OperationClaim)
            .WithMany(oc => oc.UserOperationClaims)
            .HasForeignKey(uoc => uoc.OperationClaimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
