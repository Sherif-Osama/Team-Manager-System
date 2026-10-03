namespace TeamManager.Application.Features.Tasks.TaskComment.Queries.GetMyMentions
{
    public sealed record GetMyMentionsResponse(IReadOnlyCollection<GetMyMentionsItem> Items, int TotalCount, int Page = 1,
        int PageSize = 20);

    public sealed record GetMyMentionsItem(long CommentId, long TaskId, string TaskTitle, Guid AuthorUserId, string AuthorUserName,
        string Content, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
}