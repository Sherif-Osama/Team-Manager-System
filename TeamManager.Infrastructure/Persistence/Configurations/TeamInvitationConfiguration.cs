using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class TeamInvitationConfiguration : IEntityTypeConfiguration<TeamInvitation>
{
    public void Configure(EntityTypeBuilder<TeamInvitation> builder)
    {
        builder.ToTable("TeamInvitations", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_TeamInvitations_Status", "[Status] BETWEEN 1 AND 5");
        });

        builder.HasKey(x => x.Id).HasName("PK_TeamInvitations").IsClustered();

        builder.Property(x => x.Id).HasColumnName("TeamInvitationId").ValueGeneratedOnAdd().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.InvitedEmail).IsRequired().HasMaxLength(256);

        builder.Property(x => x.TeamRole).HasColumnName("TeamRoleId").IsRequired().HasConversion<byte>();

        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(256);

        builder.Property(x => x.Status).IsRequired().HasConversion<byte>().HasDefaultValue(TeamInvitationStatus.Pending);

        builder.Property(x => x.ExpiresAtUtc).IsRequired().HasColumnType("datetime2(3)");

        builder.Property(x => x.AcceptedAtUtc).HasColumnType("datetime2(3)");

        builder.Property(x => x.RejectedAtUtc).HasColumnType("datetime2(3)");

        builder.Property(x => x.CancelledAtUtc).HasColumnType("datetime2(3)");

        builder.Property(x => x.CreatedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.Team).WithMany(x => x.Invitations).HasForeignKey(x => x.TeamId)
            .HasConstraintName("FK_TeamInvitations_Teams").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.InvitedUser).WithMany().HasForeignKey(x => x.InvitedUserId)
            .HasConstraintName("FK_TeamInvitations_InvitedUser").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.InvitedByUser).WithMany().HasForeignKey(x => x.InvitedBy)
            .HasConstraintName("FK_TeamInvitations_InvitedBy").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("UQ_TeamInvitations_TokenHash");

        builder.HasIndex(x => new { x.TeamId, x.InvitedEmail }).IsUnique().HasDatabaseName("UQ_TeamInvitations_TeamId_Email_Pending")
            .HasFilter("[Status] = 1");

        builder.HasIndex(x => new { x.TeamId, x.Status }).HasDatabaseName("IX_TeamInvitations_TeamId_Status");
    }
}
