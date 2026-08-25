using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Projects_Status", "[Status] BETWEEN 1 AND 4");
            tableBuilder.HasCheckConstraint("CK_Projects_DateRange", "[DueDate] IS NULL OR [StartDate] IS NULL OR [DueDate] >= [StartDate]");
        });

        builder.HasKey(x => x.Id).HasName("PK_Projects").IsClustered();

        builder.Property(x => x.Id).HasColumnName("ProjectId").ValueGeneratedOnAdd().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);

        builder.Property(x => x.Description).HasMaxLength(1000);

        builder.Property(x => x.Status).IsRequired().HasConversion<byte>().HasDefaultValue(ProjectStatus.Active);

        builder.Property(x => x.StartDate).HasColumnType("date");

        builder.Property(x => x.DueDate).HasColumnType("date");

        builder.Property(x => x.CreatedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");

        builder.Property(x => x.DeletedAtUtc).HasColumnType("datetime2(3)");

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasOne(x => x.Team).WithMany(x => x.Projects).HasForeignKey(x => x.TeamId)
            .HasConstraintName("FK_Projects_Teams").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerUserId)
            .HasConstraintName("FK_Projects_Owner").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy)
            .HasConstraintName("FK_Projects_CreatedBy").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.TeamId, x.Status }).HasDatabaseName("IX_Projects_TeamId_Status")
            .HasFilter("[DeletedAtUtc] IS NULL");

        builder.Navigation(x => x.Members).HasField("_members").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.Tasks).HasField("_tasks").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
