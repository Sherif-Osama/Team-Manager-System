namespace TeamManager.Application.Features.Tasks.TaskComment.Queries.GetTaskComments
{
    public sealed record GetTaskCommentsResponse(IReadOnlyCollection<GetTaskCommentsItem> Items,
        int TotalCount, int Page = 1, int PageSize = 20);

    public sealed record GetTaskCommentsItem(long Id, Guid AuthorUserId, string AuthorUserName, string Content,
        DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, IReadOnlyCollection<CommentMentionResponse> Mentions);

    public sealed record CommentMentionResponse(Guid UserId, string UserName);
}