namespace TeamManager.Application.Features.Teams.Queries.GetTeamByName
{
    public sealed record GetTeamByNameResponse(Guid Id, string Name, string? Description, bool IsActive,
        Guid OwnerUserId, Guid CreatedBy, DateTime CreatedAtUtc);
}
