using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> builder)
    {
        builder.ToTable("TaskComments");

        builder.HasKey(x => x.Id).HasName("PK_TaskComments").IsClustered();

        builder.Property(x => x.Id).HasColumnName("TaskCommentId").ValueGeneratedOnAdd().UseIdentityColumn();

        builder.Property(x => x.Content).IsRequired().HasColumnType("nvarchar(max)");

        builder.Property(x => x.CreatedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");

        builder.Property(x => x.DeletedAtUtc).HasColumnType("datetime2(3)");

        builder.HasOne(x => x.Task).WithMany(x => x.Comments).HasForeignKey(x => x.TaskId)
            .HasConstraintName("FK_TaskComments_Tasks").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorUserId)
            .HasConstraintName("FK_TaskComments_Author").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.TaskId, x.CreatedAtUtc }).HasDatabaseName("IX_TaskComments_TaskId_CreatedAtUtc")
            .HasFilter("[DeletedAtUtc] IS NULL");

        builder.Navigation(x => x.Mentions).HasField("_mentions").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
