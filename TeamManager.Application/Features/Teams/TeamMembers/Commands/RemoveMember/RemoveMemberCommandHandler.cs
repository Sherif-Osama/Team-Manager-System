using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TeamExceptions;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.RemoveMember
{
    public sealed class RemoveMemberCommandHandler(ITeamRepository teamRepository, ICurrentUser currentUser, IUnitOfWork unitOfWork)
        : IRequestHandler<RemoveMemberCommand>
    {
        public async Task Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var team = await teamRepository.GetByIdWithMembersAsync(request.TeamId, cancellationToken);
            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                team.RemoveMember(request.MemberId, currentUser.UserId!.Value);
            }, cancellationToken);
        }
    }
}