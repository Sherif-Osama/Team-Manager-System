using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Domain.Enums;

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

            var memberExists = team.Members.Any(m => m.Id == request.MemberId && m.Status == TeamMemberStatus.Active);

            if (!memberExists)
                throw new TeamMemberNotFoundException(request.TeamId, request.MemberId);

            team.ChangeMemberRole(request.MemberId, request.Role);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}