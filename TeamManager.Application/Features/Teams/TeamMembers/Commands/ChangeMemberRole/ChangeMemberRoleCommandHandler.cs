using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TeamExceptions;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.ChangeMemberRole
{
    public sealed class ChangeMemberRoleCommandHandler(ITeamRepository teamRepository, ICurrentUser currentUser, IUnitOfWork unitOfWork)
        : IRequestHandler<ChangeMemberRoleCommand>
    {
        public async Task Handle(ChangeMemberRoleCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var team = await teamRepository.GetByIdWithMembersAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            team.ChangeMemberRole(request.MemberId, request.Role, currentUser.UserId.Value);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}