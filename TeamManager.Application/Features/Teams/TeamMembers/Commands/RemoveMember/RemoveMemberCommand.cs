using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.RemoveMember
{
    public sealed record RemoveMemberCommand(Guid TeamId, long MemberId) : IRequest, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => new[] { TeamRole.Owner, TeamRole.Admin };
    }
}