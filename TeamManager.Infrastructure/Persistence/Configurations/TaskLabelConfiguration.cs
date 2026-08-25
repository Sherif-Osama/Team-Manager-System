using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class TaskLabelConfiguration : IEntityTypeConfiguration<TaskLabel>
{
    public void Configure(EntityTypeBuilder<TaskLabel> builder)
    {
        builder.ToTable("TaskLabels");

        builder.Ignore(x => x.Id);

        builder.HasKey(x => new { x.TaskId, x.LabelId }).HasName("PK_TaskLabels").IsClustered();

        builder.Property(x => x.AddedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.Task).WithMany(x => x.Labels).HasForeignKey(x => x.TaskId)
            .HasConstraintName("FK_TaskLabels_Tasks").OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Label).WithMany(x => x.TaskLabels).HasForeignKey(x => x.LabelId)
            .HasConstraintName("FK_TaskLabels_Labels").OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LabelId).HasDatabaseName("IX_TaskLabels_LabelId");
    }
}
