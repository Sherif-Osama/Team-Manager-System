namespace TeamManager.Application.Features.ActivityLog.Queries.GetTeamActivity
{
    public sealed record GetTeamActivityRequest(int Page = 1, int PageSize = 20);
}