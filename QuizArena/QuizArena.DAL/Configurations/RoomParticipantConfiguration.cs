using QuizArena.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuizArena.DAL.Configurations;

public sealed class RoomParticipantConfiguration : IEntityTypeConfiguration<RoomParticipant>
{
    public void Configure(EntityTypeBuilder<RoomParticipant> builder)
    {
        builder.ToTable("RoomParticipants");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Role).HasConversion<int>();

        // Bir kullanıcı aynı odaya iki kez katılamaz. Bu kısıt olmadan
        // istemci "katıl" isteğini iki kez göndererek odada iki kez görünür
        // ve iki ayrı skor üretirdi.
        builder.HasIndex(p => new { p.RoomId, p.UserId })
            .IsUnique()
            .HasDatabaseName("UX_RoomParticipants_Room_User");

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Competitions)
            .WithOne(c => c.RoomParticipant)
            .HasForeignKey(c => c.RoomParticipantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
