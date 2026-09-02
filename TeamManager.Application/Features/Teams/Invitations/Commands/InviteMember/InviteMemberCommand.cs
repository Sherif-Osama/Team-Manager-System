using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.InviteMember
{
    public sealed record InviteMemberCommand(Guid TeamId, string Email, TeamRole TeamRole) : IRequest<Guid>,
        ITeamScopedRequest, IRequiresConfirmedEmail
    {
        public TeamRole[] RequiredRoles => new[] { TeamRole.Owner, TeamRole.Admin };
    }
}