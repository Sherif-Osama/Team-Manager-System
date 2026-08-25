using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Notifications_Type", "[Type] BETWEEN 1 AND 5");
        });

        builder.HasKey(x => x.Id).HasName("PK_Notifications").IsClustered();

        builder.Property(x => x.Id).HasColumnName("NotificationId").ValueGeneratedOnAdd().UseIdentityColumn();

        builder.Property(x => x.Type).IsRequired().HasConversion<byte>();

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);

        builder.Property(x => x.Body).HasMaxLength(500);

        builder.Property(x => x.RelatedEntityType).HasMaxLength(30);

        builder.Property(x => x.RelatedEntityId).HasMaxLength(50);

        builder.Property(x => x.IsRead).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.ReadAtUtc).HasColumnType("datetime2(3)");

        builder.Property(x => x.CreatedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.Recipient).WithMany().HasForeignKey(x => x.RecipientUserId)
            .HasConstraintName("FK_Notifications_Recipient").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.RecipientUserId, x.IsRead, x.CreatedAtUtc })
            .HasDatabaseName("IX_Notifications_RecipientUserId_IsRead_CreatedAtUtc")
            .IsDescending(false, false, true);
    }
}
