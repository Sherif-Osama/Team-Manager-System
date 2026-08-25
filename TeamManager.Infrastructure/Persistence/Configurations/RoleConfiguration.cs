using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(x => x.Id).HasName("PK_Roles").IsClustered();

        builder.Property(x => x.Id).HasColumnName("RoleId").ValueGeneratedOnAdd().UseIdentityColumn();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);

        builder.Property(x => x.Description).HasMaxLength(256);

        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UQ_Roles_Name");

        builder.Navigation(x => x.RolePermissions).HasField("_rolePermissions").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.UserRoles).HasField("_userRoles").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasData(
            new { Id = 1, Name = "SystemAdmin", Description = "Full administrative access to the platform" },
            new { Id = 2, Name = "User", Description = "Standard authenticated user" }
        );
    }
}
