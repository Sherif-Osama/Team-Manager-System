using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");

        builder.HasKey(x => x.Id).HasName("PK_Teams").IsClustered();

        builder.Property(x => x.Id).HasColumnName("TeamId").ValueGeneratedOnAdd().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);

        builder.HasIndex(x => x.Name).IsUnique().HasFilter("[DeletedAtUtc] IS NULL").HasDatabaseName("UQ_Teams_Name");

        builder.Property(x => x.Description).HasMaxLength(500);

        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.CreatedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");

        builder.Property(x => x.DeletedAtUtc).HasColumnType("datetime2(3)");

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerUserId)
            .HasConstraintName("FK_Teams_Users_Owner").OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy)
            .HasConstraintName("FK_Teams_Users_CreatedBy").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.OwnerUserId).HasDatabaseName("IX_Teams_OwnerUserId").HasFilter("[DeletedAtUtc] IS NULL");

        builder.Navigation(x => x.Members).HasField("_members").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.Invitations).HasField("_invitations").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.Projects).HasField("_projects").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.Labels).HasField("_labels").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
