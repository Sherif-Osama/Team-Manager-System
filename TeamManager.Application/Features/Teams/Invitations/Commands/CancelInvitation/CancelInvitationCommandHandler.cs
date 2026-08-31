using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.CancelInvitation
{
    public sealed class CancelInvitationCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<CancelInvitationCommand>
    {
        public async Task Handle(CancelInvitationCommand request, CancellationToken cancellationToken)
        {
            var team = await teamRepository.GetByIdWithInvitationsAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            team.CancelInvitation(request.InvitationId);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}