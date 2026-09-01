using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.ChangeMemberRole
{
    public sealed class ChangeMemberRoleCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<ChangeMemberRoleCommand>
    {
        public async Task Handle(ChangeMemberRoleCommand request, CancellationToken cancellationToken)
        {
            var team = await teamRepository.GetByIdWithMembersAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            team.ChangeMemberRole(request.MemberId, request.Role);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}