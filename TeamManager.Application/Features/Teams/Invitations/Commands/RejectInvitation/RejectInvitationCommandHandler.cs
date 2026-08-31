using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.RejectInvitation
{
    public sealed class RejectInvitationCommandHandler(ITeamRepository teamRepository, ICurrentUser currentUser,
        IInvitationTokenService invitationTokenService, IUnitOfWork unitOfWork)
        : IRequestHandler<RejectInvitationCommand>
    {
        public async Task Handle(RejectInvitationCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue || string.IsNullOrEmpty(currentUser.Email))
                throw new UnauthorizedAccessException("User is not authenticated.");

            var tokenHash = invitationTokenService.HashToken(request.Token);

            var team = await teamRepository.GetByInvitationTokenHashAsync(tokenHash, cancellationToken);

            if (team is null)
                throw new InvitationNotFoundException();

            team.RejectInvitation(tokenHash, currentUser.UserId.Value, currentUser.Email!);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
