namespace TeamManager.Application.Features.Teams.Queries.GetTeams
{
    public sealed record GetTeamsResponse(IReadOnlyCollection<TeamListItem> Items, int Page, int PageSize, int TotalCount);

    public sealed record TeamListItem(Guid Id, string Name, string? Description, Guid OwnerUserId,
        string OwnerName, Guid CreatedBy, string CreatorName, DateTime CreatedAtUtc);
}