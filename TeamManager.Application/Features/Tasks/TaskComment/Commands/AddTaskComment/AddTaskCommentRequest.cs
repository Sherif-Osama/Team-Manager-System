namespace TeamManager.Application.Features.Tasks.TaskComment.Commands.AddTaskComment
{
    public sealed record AddTaskCommentRequest(string Content, IReadOnlyCollection<Guid>? MentionedUserIds = null);
}