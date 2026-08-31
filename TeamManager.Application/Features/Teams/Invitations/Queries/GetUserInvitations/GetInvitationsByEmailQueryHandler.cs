using MediatR;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetUserInvitations
{
    public sealed class GetInvitationsByEmailQueryHandler(ITeamRepository teamRepository)
        : IRequestHandler<GetInvitationsByEmailQuery, GetInvitationsByEmailResponse>
    {
        public Task<GetInvitationsByEmailResponse> Handle(GetInvitationsByEmailQuery request, CancellationToken cancellationToken)
        {
            return teamRepository.GetInvitationsByEmailAsync(request, cancellationToken);
        }
    }
}