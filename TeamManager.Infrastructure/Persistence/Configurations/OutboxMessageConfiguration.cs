using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Infrastructure.Persistence.Outbox;

namespace TeamManager.Infrastructure.Persistence.Configurations
{
    public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");

            builder.HasKey(x => x.Id).HasName("PK_OutboxMessages");

            builder.Property(x => x.Id).HasColumnName("OutboxMessageId").ValueGeneratedOnAdd();

            builder.Property(x => x.Type).IsRequired().HasMaxLength(200);

            builder.Property(x => x.Payload).IsRequired().HasColumnType("nvarchar(max)");

            builder.Property(x => x.OccurredOnUtc).IsRequired().HasColumnType("datetime2(3)");

            builder.Property(x => x.ProcessedOnUtc)
                .HasColumnType("datetime2(3)");

            builder.Property(x => x.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.NextAttemptOnUtc).HasColumnType("datetime2(3)");

            builder.Property(x => x.Error).HasColumnType("nvarchar(max)");

            builder.HasIndex(x => new
            {
                x.ProcessedOnUtc,
                x.NextAttemptOnUtc
            })
            .HasDatabaseName("IX_OutboxMessages_Processing");

            builder.Property(x => x.FailedOnUtc).HasColumnType("datetime2(3)");
        }
    }
}