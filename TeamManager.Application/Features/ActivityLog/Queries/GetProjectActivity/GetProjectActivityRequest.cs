namespace TeamManager.Application.Features.ActivityLog.Queries.GetProjectActivity
{
    public sealed record GetProjectActivityRequest(int Page = 1, int PageSize = 20);
}