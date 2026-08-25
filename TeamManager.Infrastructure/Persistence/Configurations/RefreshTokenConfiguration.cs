using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(x => x.Id).HasName("PK_RefreshTokens").IsClustered();

        builder.Property(x => x.Id).HasColumnName("RefreshTokenId").ValueGeneratedOnAdd().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(256);

        builder.Property(x => x.DeviceInfo).HasMaxLength(256);

        builder.Property(x => x.IpAddress).HasColumnType("varchar(45)");

        builder.Property(x => x.ExpiresAtUtc).IsRequired().HasColumnType("datetime2(3)");

        builder.Property(x => x.RevokedAtUtc).HasColumnType("datetime2(3)");

        builder.Property(x => x.ReplacedByTokenId).IsRequired(false);

        builder.Property(x => x.CreatedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_RefreshTokens_Users").OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ReplacedByToken).WithMany().HasForeignKey(x => x.ReplacedByTokenId)
            .HasConstraintName("FK_RefreshTokens_RefreshTokens").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("UQ_RefreshTokens_TokenHash");

        builder.HasIndex(x => new
        {
            x.UserId,
            x.ExpiresAtUtc
        }).HasDatabaseName("IX_RefreshTokens_UserId_Active").HasFilter("[RevokedAtUtc] IS NULL");
    }
}