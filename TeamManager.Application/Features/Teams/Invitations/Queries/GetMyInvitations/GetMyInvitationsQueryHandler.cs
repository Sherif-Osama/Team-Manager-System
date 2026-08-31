using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetMyInvitations
{
    public sealed class GetMyInvitationsQueryHandler(ICurrentUser currentUser, ITeamRepository teamRepository)
        : IRequestHandler<GetMyInvitationsQuery, GetMyInvitationsResponse>
    {
        public async Task<GetMyInvitationsResponse> Handle(GetMyInvitationsQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(currentUser.Email))
                throw new UnauthorizedAccessException("User email is not available.");

            return await teamRepository.GetMyInvitationsAsync(currentUser.Email, request, cancellationToken);
        }
    }
}