using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.ChangeMemberRole
{
    public sealed record ChangeMemberRoleRequest(TeamRole Role);
}