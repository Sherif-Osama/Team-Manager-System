using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id).HasName("PK_AuditLogs").IsClustered();

        builder.Property(x => x.Id).HasColumnName("AuditLogId").ValueGeneratedOnAdd().UseIdentityColumn();

        builder.Property(x => x.Action).IsRequired().HasMaxLength(100);

        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(50);

        builder.Property(x => x.EntityId).IsRequired().HasMaxLength(50);

        builder.Property(x => x.Details).HasColumnType("nvarchar(max)");

        builder.Property(x => x.IpAddress).HasColumnType("varchar(45)");

        builder.Property(x => x.CreatedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorUserId)
            .HasConstraintName("FK_AuditLogs_ActorUser").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAtUtc })
            .HasDatabaseName("IX_AuditLogs_EntityType_EntityId")
            .IsDescending(false, false, true);

        builder.HasIndex(x => x.CreatedAtUtc).HasDatabaseName("IX_AuditLogs_CreatedAtUtc").IsDescending();
    }
}
