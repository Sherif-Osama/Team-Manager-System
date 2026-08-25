using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");

        builder.Ignore(x => x.Id);

        builder.HasKey(x => new { x.UserId, x.NotificationType }).HasName("PK_NotificationPreferences").IsClustered();

        builder.Property(x => x.NotificationType).HasColumnName("NotificationType").IsRequired().HasConversion<byte>();

        builder.Property(x => x.IsEnabled).IsRequired().HasDefaultValue(true);

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_NotificationPreferences_Users").OnDelete(DeleteBehavior.Cascade);
    }
}
