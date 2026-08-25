using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_ProjectMembers_Status", "[Status] IN (1, 2)");
        });

        builder.HasKey(x => x.Id).HasName("PK_ProjectMembers").IsClustered();

        builder.Property(x => x.Id).HasColumnName("ProjectMemberId").ValueGeneratedOnAdd().UseIdentityColumn();

        builder.Property(x => x.TeamRole).HasColumnName("TeamRoleId").IsRequired().HasConversion<byte>();

        builder.Property(x => x.Status).IsRequired().HasConversion<byte>().HasDefaultValue(ProjectMemberStatus.Active);

        builder.Property(x => x.AddedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.RemovedAtUtc).HasColumnType("datetime2(3)");

        builder.HasOne(x => x.Project).WithMany(x => x.Members).HasForeignKey(x => x.ProjectId)
            .HasConstraintName("FK_ProjectMembers_Projects").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_ProjectMembers_Users").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.AddedByUser).WithMany().HasForeignKey(x => x.AddedBy)
            .HasConstraintName("FK_ProjectMembers_AddedBy").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.ProjectId, x.UserId }).IsUnique().HasDatabaseName("UQ_ProjectMembers_ProjectId_UserId_Active")
            .HasFilter("[Status] = 1");

        builder.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("IX_ProjectMembers_UserId_Status")
            .IncludeProperties(x => new { x.ProjectId });
    }
}
