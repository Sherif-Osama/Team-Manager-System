using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Users_FailedLoginAttempts", "[FailedLoginAttempts] >= 0");
        });

        builder.HasKey(x => x.Id).HasName("PK_Users").IsClustered();

        builder.Property(x => x.Id).HasColumnName("UserId").ValueGeneratedOnAdd().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.Email).IsRequired().HasMaxLength(256);

        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(100);

        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(256);

        builder.Property(x => x.IsEmailConfirmed).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.FailedLoginAttempts).IsRequired().HasConversion<short>().HasDefaultValue(0);

        builder.Property(x => x.LockoutEndUtc).HasColumnType("datetime2(3)");

        builder.Property(x => x.LastLoginUtc).HasColumnType("datetime2(3)");

        builder.Property(x => x.CreatedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");

        builder.Property(x => x.DeletedAtUtc).HasColumnType("datetime2(3)");

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_Users_Email");

        builder.HasIndex(x => x.IsActive).HasDatabaseName("IX_Users_IsActive").HasFilter("[DeletedAtUtc] IS NULL");

        builder.Navigation(x => x.RefreshTokens).HasField("_refreshTokens").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.UserRoles).HasField("_userRoles").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(x => x.EmailConfirmationTokenHash).HasMaxLength(256);

        builder.Property(x => x.EmailConfirmationTokenExpiresAtUtc).HasColumnType("datetime2(3)");

        builder.Property(x => x.PendingEmail).HasMaxLength(256);
    }
}