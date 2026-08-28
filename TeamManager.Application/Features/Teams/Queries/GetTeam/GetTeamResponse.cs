namespace TeamManager.Application.Features.Teams.Queries.GetTeam
{
    public sealed record GetTeamResponse(Guid Id, string Name, string? Description, bool IsActive,
        Guid OwnerUserId, string OwnerName, Guid CreatedBy,
        string CreatorName, int MembersCount, int ProjectsCount, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc
);
}