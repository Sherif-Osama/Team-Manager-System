using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.AddMember
{
    public sealed record AddMemberCommand(Guid TeamId, Guid UserId, TeamRole TeamRole) : IRequest<long>, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner, TeamRole.Admin];
    };
}