using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.RemoveMember
{
    public sealed class RemoveMemberCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork, ICurrentUser currentUser)
        : IRequestHandler<RemoveMemberCommand>
    {
        public async Task Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
        {
            var team = await teamRepository.GetByIdWithMembersAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            var member = team.Members.FirstOrDefault(x => x.Id == request.MemberId && x.Status == TeamMemberStatus.Active);

            if (member is null)
                throw new TeamMemberNotFoundException(request.TeamId, request.MemberId);

            team.RemoveMember(member.UserId, currentUser.UserId!.Value);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}