namespace TeamManager.Application.Features.Tasks.TaskComment.Queries.GetTaskComments
{
    public sealed record GetTaskCommentsRequest(int Page = 1, int PageSize = 20);
}