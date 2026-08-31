using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Communication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.InviteMember
{
    public sealed class InviteMemberCommandHandler(ITeamRepository teamRepository, IUserRepository userRepository,
        IUnitOfWork unitOfWork, ICurrentUser currentUser,
        IInvitationTokenService invitationTokenService, IEmailSender emailSender)
        : IRequestHandler<InviteMemberCommand, Guid>
    {
        public async Task<Guid> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
        {
            var team = await teamRepository.GetByIdWithMembersAndInvitationsAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

            var token = invitationTokenService.GenerateToken();

            var tokenHash = invitationTokenService.HashToken(token);

            var invitation = team.Invite(request.Email, user?.Id, currentUser.UserId!.Value,
                request.TeamRole, tokenHash, DateTime.UtcNow.AddDays(7));

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await emailSender.SendInvitationEmailAsync(request.Email, token, cancellationToken);

            return invitation.Id;
        }
    }
}