using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TeamExceptions;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.CancelInvitation
{
    public sealed class CancelInvitationCommandHandler(ITeamRepository teamRepository, ICurrentUser currentUser, IUnitOfWork unitOfWork)
        : IRequestHandler<CancelInvitationCommand>
    {
        public async Task Handle(CancelInvitationCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var team = await teamRepository.GetByIdWithInvitationsAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            team.CancelInvitation(request.InvitationId, currentUser.UserId.Value);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}