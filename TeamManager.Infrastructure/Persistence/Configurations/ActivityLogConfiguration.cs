using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");

        builder.HasKey(x => x.Id).HasName("PK_ActivityLogs").IsClustered();

        builder.Property(x => x.Id).HasColumnName("ActivityLogId").ValueGeneratedOnAdd().UseIdentityColumn();

        builder.Property(x => x.ActivityType).IsRequired().HasMaxLength(50);

        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(50);

        builder.Property(x => x.EntityId).IsRequired().HasMaxLength(50);

        builder.Property(x => x.Metadata).HasColumnType("nvarchar(max)");

        builder.Property(x => x.CreatedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId)
            .HasConstraintName("FK_ActivityLogs_Teams").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId)
            .HasConstraintName("FK_ActivityLogs_Projects").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorUserId)
            .HasConstraintName("FK_ActivityLogs_Actor").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.TeamId, x.CreatedAtUtc }).HasDatabaseName("IX_ActivityLogs_TeamId_CreatedAtUtc")
            .IsDescending(false, true);

        builder.HasIndex(x => new { x.ProjectId, x.CreatedAtUtc }).HasDatabaseName("IX_ActivityLogs_ProjectId_CreatedAtUtc")
            .IsDescending(false, true)
            .HasFilter("[ProjectId] IS NOT NULL");
    }
}
