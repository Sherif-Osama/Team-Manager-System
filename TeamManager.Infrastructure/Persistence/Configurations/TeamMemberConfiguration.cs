using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("TeamMembers", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_TeamMembers_Status", "[Status] IN (1, 2)");
        });

        builder.HasKey(x => x.Id).HasName("PK_TeamMembers").IsClustered();

        builder.Property(x => x.Id).HasColumnName("TeamMemberId").ValueGeneratedOnAdd().UseIdentityColumn();

        builder.Property(x => x.TeamRole).HasColumnName("TeamRoleId").IsRequired().HasConversion<byte>();

        builder.Property(x => x.Status).IsRequired().HasConversion<byte>().HasDefaultValue(TeamMemberStatus.Active);

        builder.Property(x => x.JoinedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.RemovedAtUtc).HasColumnType("datetime2(3)");

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasOne(x => x.Team).WithMany(x => x.Members).HasForeignKey(x => x.TeamId)
            .HasConstraintName("FK_TeamMembers_Teams").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_TeamMembers_Users").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.InvitedByUser).WithMany().HasForeignKey(x => x.InvitedBy)
            .HasConstraintName("FK_TeamMembers_InvitedBy").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.RemovedByUser).WithMany().HasForeignKey(x => x.RemovedBy)
            .HasConstraintName("FK_TeamMembers_RemovedBy").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.TeamId, x.UserId }).IsUnique().HasDatabaseName("UQ_TeamMembers_TeamId_UserId_Active")
            .HasFilter("[Status] = 1");

        builder.HasIndex(x => new { x.TeamId, x.Status }).HasDatabaseName("IX_TeamMembers_TeamId_Status")
            .IncludeProperties(x => new { x.UserId, x.TeamRole });

        builder.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("IX_TeamMembers_UserId_Status")
            .IncludeProperties(x => new { x.TeamId, x.TeamRole });
    }
}
