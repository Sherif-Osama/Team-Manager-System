namespace TeamManager.Application.Features.AuditLogs.Queries
{
    public sealed record GetAuditLogsResponse(IReadOnlyList<AuditLogItem> Items, int Page, int PageSize, int TotalCount);

    public sealed record AuditLogItem(long Id, string Action, string EntityType, string EntityId, Guid? ActorUserId, string? ActorName,
        string? Details, string? IpAddress, DateTime CreatedAtUtc);
}