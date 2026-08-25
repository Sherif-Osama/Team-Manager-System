using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(x => x.Id).HasName("PK_Permissions").IsClustered();

        builder.Property(x => x.Id).HasColumnName("PermissionId").ValueGeneratedOnAdd().UseIdentityColumn();

        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);

        builder.Property(x => x.Description).HasMaxLength(256);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UQ_Permissions_Code");

        builder.Navigation(x => x.RolePermissions).HasField("_rolePermissions").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasData(
            new { Id = 1, Code = "system.manage_users", Description = "Disable/enable or delete any user account" },
            new { Id = 2, Code = "system.view_audit_log", Description = "View platform-wide audit log" },
            new { Id = 3, Code = "system.manage_roles", Description = "Assign or revoke system roles" }
        );
    }
}
