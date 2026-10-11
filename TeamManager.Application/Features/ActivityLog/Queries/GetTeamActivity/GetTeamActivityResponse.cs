namespace TeamManager.Application.Features.ActivityLog.Queries.GetTeamActivity
{
    public sealed record GetTeamActivityResponse(IReadOnlyCollection<GetTeamActivityItem> Items, int TotalCount,
        int Page = 1, int PageSize = 20);

    public sealed record GetTeamActivityItem(long Id, string ActivityType, Guid ActorUserId, string ActorName,
        Guid? ProjectId, string? ProjectName, string EntityType, string EntityId, string? Metadata, DateTime CreatedAtUtc);
}