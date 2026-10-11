namespace TeamManager.Application.Features.ActivityLog.Queries.GetProjectActivity
{
    public sealed record GetProjectActivityResponse(IReadOnlyCollection<GetProjectActivityItem> Items, int TotalCount, int Page,
        int PageSize);

    public sealed record GetProjectActivityItem(long Id, string ActivityType, Guid ActorUserId, string ActorName, string EntityType,
        string EntityId, string? Metadata, DateTime CreatedAtUtc);
}