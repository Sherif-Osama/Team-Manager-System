using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class TaskChecklistItemConfiguration : IEntityTypeConfiguration<TaskChecklistItem>
{
    public void Configure(EntityTypeBuilder<TaskChecklistItem> builder)
    {
        builder.ToTable("TaskChecklistItems");

        builder.HasKey(x => x.Id).HasName("PK_TaskChecklistItems").IsClustered();

        builder.Property(x => x.Id).HasColumnName("TaskChecklistItemId").ValueGeneratedOnAdd().UseIdentityColumn();

        builder.Property(x => x.Content).IsRequired().HasMaxLength(300);

        builder.Property(x => x.IsCompleted).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.SortOrder).IsRequired().HasDefaultValue((short)0);

        builder.Property(x => x.CompletedAtUtc).HasColumnType("datetime2(3)");

        builder.Property(x => x.CreatedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.Task).WithMany(x => x.ChecklistItems).HasForeignKey(x => x.TaskId)
            .HasConstraintName("FK_TaskChecklistItems_Tasks").OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CompletedByUser).WithMany().HasForeignKey(x => x.CompletedBy)
            .HasConstraintName("FK_TaskChecklistItems_CompletedBy").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.TaskId, x.SortOrder }).HasDatabaseName("IX_TaskChecklistItems_TaskId_SortOrder");
    }
}
