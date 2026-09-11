using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;
namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.ChangeMemberRole
{
    public sealed record ChangeMemberRoleCommand(Guid TeamId, long MemberId, TeamRole Role) : IRequest, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner, TeamRole.Admin];
    }
}
