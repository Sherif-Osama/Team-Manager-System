using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Team.Commands.TransferOwnership
{
    public sealed class TransferOwnershipCommandHandler(ITeamRepository teamRepository, IUserRepository userRepository,
            IUnitOfWork unitOfWork) : IRequestHandler<TransferOwnershipCommand>
    {

        public async Task Handle(TransferOwnershipCommand request, CancellationToken cancellationToken)
        {

            var team = await teamRepository.GetByIdWithMembersAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            var newOwner = await userRepository.GetByIdAsync(request.NewOwnerUserId, cancellationToken);

            if (newOwner is null || !newOwner.IsActive)
                throw new UserNotFoundException(request.NewOwnerUserId);

            team.TransferOwnership(request.NewOwnerUserId);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
