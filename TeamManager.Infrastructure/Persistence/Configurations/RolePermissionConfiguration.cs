using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.Ignore(x => x.Id);

        builder.HasKey(x => new { x.RoleId, x.PermissionId }).HasName("PK_RolePermissions").IsClustered();

        builder.Property(x => x.RoleId).HasColumnName("RoleId");

        builder.Property(x => x.PermissionId).HasColumnName("PermissionId");

        builder.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId)
            .HasConstraintName("FK_RolePermissions_Roles").OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId)
            .HasConstraintName("FK_RolePermissions_Permissions").OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            new { RoleId = 1, PermissionId = 1 },
            new { RoleId = 1, PermissionId = 2 },
            new { RoleId = 1, PermissionId = 3 }
        );
    }
}
