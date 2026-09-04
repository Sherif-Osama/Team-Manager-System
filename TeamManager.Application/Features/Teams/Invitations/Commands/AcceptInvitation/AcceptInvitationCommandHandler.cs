using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.AcceptInvitation
{
    public sealed class AcceptInvitationCommandHandler(ITeamRepository teamRepository, ICurrentUser currentUser,
        IInvitationTokenService invitationTokenService, IUnitOfWork unitOfWork) : IRequestHandler<AcceptInvitationCommand>
    {
        public async Task Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue || string.IsNullOrEmpty(currentUser.Email))
                throw new UnauthorizedAccessException("User is not authenticated.");

            var tokenHash = invitationTokenService.HashToken(request.Token);

            var team = await teamRepository.GetByInvitationTokenHashWithMemberAsync(tokenHash, currentUser.UserId.Value,
                cancellationToken);

            if (team is null)
                throw new InvitationNotFoundException();

            try
            {
                team.AcceptInvitation(tokenHash, currentUser.UserId.Value, currentUser.Email!);

                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DomainException)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw;
            }
        }
    }
}
