namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeams
{
    public sealed record GetTeamsResponse(IReadOnlyCollection<TeamListItem> Items, int TotalCount, int Page = 1, int PageSize = 20);

    public sealed record TeamListItem(Guid Id, string Name, string? Description, Guid OwnerUserId,
        string OwnerName, Guid CreatedBy, string CreatorName, DateTime CreatedAtUtc);
}