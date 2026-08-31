using MediatR;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetInvitations
{

    public sealed class GetInvitationsQueryHandler(ITeamRepository teamRepository)
        : IRequestHandler<GetInvitationsQuery, GetInvitationsResponse>
    {
        public Task<GetInvitationsResponse> Handle(GetInvitationsQuery request, CancellationToken cancellationToken)
        {
            return teamRepository.GetInvitationsAsync(request, cancellationToken);
        }
    }
}