namespace TeamManager.Application.Features.Tasks.TaskComment.Queries.GetMyComments
{
    public sealed record GetMyCommentsResponse(IReadOnlyCollection<GetMyCommentsItem> Items, int TotalCount, int Page = 1,
        int PageSize = 20);

    public sealed record GetMyCommentsItem(long Id, long TaskId, string TaskTitle, string Content, DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc, IReadOnlyCollection<MyCommentMentionResponse> Mentions);

    public sealed record MyCommentMentionResponse(Guid UserId, string UserName);
}