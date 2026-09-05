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
            // Run ownership transfer in a serializable transaction so the validation
            // of the new owner's account state and team membership is consistent with
            // concurrent account deactivation/deletion and other ownership changes.
            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var newOwner = await userRepository.GetByIdAsync(request.NewOwnerUserId, ct);

                if (newOwner is null || !newOwner.IsActive)
                    throw new UserNotFoundException(request.NewOwnerUserId);

                var team = await teamRepository.GetByIdWithMembersAsync(request.TeamId, ct);

                if (team is null)
                    throw new TeamNotFoundException(request.TeamId);

                team.TransferOwnership(request.NewOwnerUserId);

            }, cancellationToken);
        }
    }
}