using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> builder)
    {
        builder.ToTable("TaskDependencies", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_TaskDependencies_NoSelfReference", "[TaskId] <> [DependsOnTaskId]");
        });

        builder.HasKey(x => x.Id).HasName("PK_TaskDependencies").IsClustered();

        builder.Property(x => x.Id).HasColumnName("TaskDependencyId").ValueGeneratedOnAdd().UseIdentityColumn();

        builder.Property(x => x.CreatedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.Task).WithMany(x => x.Dependencies).HasForeignKey(x => x.TaskId)
            .HasConstraintName("FK_TaskDependencies_Task").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.DependsOnTask).WithMany().HasForeignKey(x => x.DependsOnTaskId)
            .HasConstraintName("FK_TaskDependencies_DependsOn").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy)
            .HasConstraintName("FK_TaskDependencies_CreatedBy").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.TaskId, x.DependsOnTaskId }).IsUnique().HasDatabaseName("UQ_TaskDependencies_Task_DependsOn");

        builder.HasIndex(x => x.DependsOnTaskId).HasDatabaseName("IX_TaskDependencies_DependsOnTaskId");
    }
}
