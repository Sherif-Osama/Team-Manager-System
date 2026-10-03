namespace TeamManager.Application.Features.Tasks.TaskComment.Commands.EditTaskComment
{
    public sealed record EditTaskCommentRequest(string Content, IReadOnlyCollection<Guid>? MentionedUserIds = null);
}