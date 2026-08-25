using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;
using TaskStatus = TeamManager.Domain.Enums.TaskStatus;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Tasks_Status", "[Status] BETWEEN 1 AND 5");
            tableBuilder.HasCheckConstraint("CK_Tasks_Priority", "[Priority] BETWEEN 1 AND 4");
            tableBuilder.HasCheckConstraint("CK_Tasks_DateRange", "[DueDate] IS NULL OR [StartDate] IS NULL OR [DueDate] >= [StartDate]");
        });

        builder.HasKey(x => x.Id).HasName("PK_Tasks").IsClustered();

        builder.Property(x => x.Id).HasColumnName("TaskId").ValueGeneratedOnAdd().UseIdentityColumn();

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);

        builder.Property(x => x.Description).HasColumnType("nvarchar(max)");

        builder.Property(x => x.Status).IsRequired().HasConversion<byte>().HasDefaultValue(TaskStatus.Todo);

        builder.Property(x => x.Priority).IsRequired().HasConversion<byte>().HasDefaultValue(TaskPriority.Medium);

        builder.Property(x => x.StartDate).HasColumnType("date");

        builder.Property(x => x.DueDate).HasColumnType("date");

        builder.Property(x => x.CompletedAtUtc).HasColumnType("datetime2(3)");

        builder.Property(x => x.CreatedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");

        builder.Property(x => x.DeletedAtUtc).HasColumnType("datetime2(3)");

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasOne(x => x.Project).WithMany(x => x.Tasks).HasForeignKey(x => x.ProjectId)
            .HasConstraintName("FK_Tasks_Projects").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy)
            .HasConstraintName("FK_Tasks_CreatedBy").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeUserId)
            .HasConstraintName("FK_Tasks_Assignee").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.ProjectId, x.Status }).HasDatabaseName("IX_Tasks_ProjectId_Status")
            .IncludeProperties(x => new { x.Title, x.Priority, x.AssigneeUserId, x.DueDate })
            .HasFilter("[DeletedAtUtc] IS NULL");

        builder.HasIndex(x => new { x.AssigneeUserId, x.Status }).HasDatabaseName("IX_Tasks_AssigneeUserId_Status")
            .HasFilter("[DeletedAtUtc] IS NULL");

        builder.HasIndex(x => x.DueDate).HasDatabaseName("IX_Tasks_DueDate")
            .HasFilter("[DeletedAtUtc] IS NULL AND [DueDate] IS NOT NULL");

        builder.Navigation(x => x.Dependencies).HasField("_dependencies").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.Labels).HasField("_labels").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.ChecklistItems).HasField("_checklistItems").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.Attachments).HasField("_attachments").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.Comments).HasField("_comments").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
