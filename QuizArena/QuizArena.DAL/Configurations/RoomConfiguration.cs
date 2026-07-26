using QuizArena.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuizArena.DAL.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(64);
        builder.Property(r => r.JoinCode).IsRequired().HasMaxLength(8);

        builder.Property(r => r.Description).HasMaxLength(512);

        builder.Property(r => r.Mode).HasConversion<int>();
        builder.Property(r => r.Status).HasConversion<int>();

        // Katılım kodu tekil: iki oda aynı kodu taşıyamaz, dolayısıyla
        // "kodla odaya katıl" işlemi hiçbir zaman belirsiz kalmaz.
        builder.HasIndex(r => r.JoinCode)
            .IsUnique()
            .HasDatabaseName("UX_Rooms_JoinCode");

        // Açık oda listesi sorgusu: Mode + Status + tarihe göre sırala.
        builder.HasIndex(r => new { r.Status, r.Mode, r.CreatedAtUtc })
            .HasDatabaseName("IX_Rooms_Status_Mode_Created");

        // Arka plan hizmeti her 30 saniyede bir "zamanı gelmiş etkinlik var mı?"
        // diye soruyor. Bu indeks olmasaydı o sorgu, sistem büyüdükçe tüm oda
        // tablosunu taramaya başlardı — hem de sürekli.
        builder.HasIndex(r => new { r.IsOfficialEvent, r.Status, r.ScheduledStartUtc })
            .HasDatabaseName("IX_Rooms_Event_Status_ScheduledStart");

        builder.HasOne(r => r.HostUser)
            .WithMany()
            .HasForeignKey(r => r.HostUserId)
            // Kullanıcı kaydı silinse bile oda ve geçmiş sonuçlar ayakta kalır.
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Participants)
            .WithOne(p => p.Room)
            .HasForeignKey(p => p.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // DİKKAT: Burada Cascade kullanılmıyor.
        // Room → RoomParticipant → Competition zinciri zaten Cascade.
        // Room → Competition da Cascade olsaydı SQL Server aynı tabloya iki
        // silme yolu ("multiple cascade paths") gördüğü için FK'yı reddederdi.
        builder.HasMany(r => r.Competitions)
            .WithOne(c => c.Room)
            .HasForeignKey(c => c.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(r => r.IsMultiplayer);
    }
}
