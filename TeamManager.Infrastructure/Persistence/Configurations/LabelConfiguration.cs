using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.ToTable("Labels", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Labels_ColorHex",
                "[ColorHex] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'");
        });

        builder.HasKey(x => x.Id).HasName("PK_Labels").IsClustered();

        builder.Property(x => x.Id).HasColumnName("LabelId").ValueGeneratedOnAdd().UseIdentityColumn();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);

        builder.Property(x => x.ColorHex).IsRequired().HasColumnType("char(7)").HasDefaultValue("#808080");

        builder.Property(x => x.CreatedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.Team).WithMany(x => x.Labels).HasForeignKey(x => x.TeamId)
            .HasConstraintName("FK_Labels_Teams").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.TeamId, x.Name }).IsUnique().HasDatabaseName("UQ_Labels_TeamId_Name");

        builder.Navigation(x => x.TaskLabels).HasField("_taskLabels").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
