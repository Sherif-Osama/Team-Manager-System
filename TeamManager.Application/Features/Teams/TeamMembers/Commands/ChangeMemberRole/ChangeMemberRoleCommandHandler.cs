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
            var member = await teamRepository.GetMemberForUpdateAsync(request.TeamId, request.MemberId, cancellationToken);

            if (member is null)
                throw new TeamMemberNotFoundException(request.TeamId, request.MemberId);

            member.ChangeRole(request.Role);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
