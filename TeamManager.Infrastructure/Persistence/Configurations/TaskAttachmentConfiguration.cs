using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class TaskAttachmentConfiguration : IEntityTypeConfiguration<TaskAttachment>
{
    public void Configure(EntityTypeBuilder<TaskAttachment> builder)
    {
        builder.ToTable("TaskAttachments", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_TaskAttachments_SizeBytes", "[SizeBytes] >= 0");
        });

        builder.HasKey(x => x.Id).HasName("PK_TaskAttachments").IsClustered();

        builder.Property(x => x.Id).HasColumnName("TaskAttachmentId").ValueGeneratedOnAdd().UseIdentityColumn();

        builder.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(260);

        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(500);

        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(150);

        builder.Property(x => x.FileHash).HasColumnType("char(64)");

        builder.Property(x => x.UploadedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.DeletedAtUtc).HasColumnType("datetime2(3)");

        builder.HasOne(x => x.Task).WithMany(x => x.Attachments).HasForeignKey(x => x.TaskId)
            .HasConstraintName("FK_TaskAttachments_Tasks").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedBy)
            .HasConstraintName("FK_TaskAttachments_UploadedBy").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.TaskId).HasDatabaseName("IX_TaskAttachments_TaskId")
            .HasFilter("[DeletedAtUtc] IS NULL");
    }
}
