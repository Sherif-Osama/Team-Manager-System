using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.RemoveMember
{
    public sealed class RemoveMemberCommandHandler(ITeamRepository teamRepository, IProjectRepository projectRepository,
        ICurrentUser currentUser, ITaskRepository taskRepository, IUnitOfWork unitOfWork) : IRequestHandler<RemoveMemberCommand>
    {
        public async Task Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var team = await teamRepository.GetByIdWithMembersAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            var memberToRemove = team.Members.FirstOrDefault(m => m.Id == request.MemberId);

            if (memberToRemove is null)
                throw new TeamMemberNotFoundException(team.Id, request.MemberId);

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                team.RemoveMember(request.MemberId, currentUser.UserId!.Value);
                await projectRepository.RemoveMembershipsByTeamAsync(team.Id, memberToRemove.UserId, ct);
                await taskRepository.UnassignActiveTasksByTeamAsync(team.Id, memberToRemove.UserId, ct);
            }, cancellationToken);
        }
    }
}