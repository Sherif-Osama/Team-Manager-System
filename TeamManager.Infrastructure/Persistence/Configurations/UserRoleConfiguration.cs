using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");

        builder.Ignore(x => x.Id);

        builder.HasKey(x => new { x.UserId, x.RoleId }).HasName("PK_UserRoles").IsClustered();

        builder.Property(x => x.AssignedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_UserRoles_Users").OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId)
            .HasConstraintName("FK_UserRoles_Roles").OnDelete(DeleteBehavior.Cascade);
    }
}
