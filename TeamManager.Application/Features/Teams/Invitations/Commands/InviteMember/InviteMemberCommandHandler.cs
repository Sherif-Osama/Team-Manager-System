using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Application.Common.Outbox;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.InviteMember
{
    public sealed class InviteMemberCommandHandler(ITeamRepository teamRepository, IUserRepository userRepository,
        IUnitOfWork unitOfWork, ICurrentUser currentUser, IInvitationTokenService invitationTokenService, IOutbox outbox)
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

            var payload = JsonSerializer.Serialize(new
            {
                To = request.Email,
                Token = token,
                InvitedBy = currentUser.UserId!.Value
            });

            outbox.Add(OutboxMessageType.InvitationEmail, payload);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return invitation.Id;
        }
    }
}