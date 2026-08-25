using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Configurations;

public class CommentMentionConfiguration : IEntityTypeConfiguration<CommentMention>
{
    public void Configure(EntityTypeBuilder<CommentMention> builder)
    {
        builder.ToTable("CommentMentions");

        builder.HasKey(x => x.Id).HasName("PK_CommentMentions").IsClustered();

        builder.Property(x => x.Id).HasColumnName("CommentMentionId").ValueGeneratedOnAdd().UseIdentityColumn();

        builder.Property(x => x.CreatedAtUtc).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.TaskComment).WithMany(x => x.Mentions).HasForeignKey(x => x.TaskCommentId)
            .HasConstraintName("FK_CommentMentions_Comments").OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MentionedUser).WithMany().HasForeignKey(x => x.MentionedUserId)
            .HasConstraintName("FK_CommentMentions_Users").OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.TaskCommentId, x.MentionedUserId }).IsUnique().HasDatabaseName("UQ_CommentMentions_Comment_User");

        builder.HasIndex(x => new { x.MentionedUserId, x.CreatedAtUtc }).HasDatabaseName("IX_CommentMentions_MentionedUserId");
    }
}
