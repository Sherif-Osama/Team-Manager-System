using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Commands.TransferOwnership
{
    public sealed class TransferOwnershipCommandHandler : IRequestHandler<TransferOwnershipCommand>
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public TransferOwnershipCommandHandler(ITeamRepository teamRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
        {
            _teamRepository = teamRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(TransferOwnershipCommand request, CancellationToken cancellationToken)
        {
            var team = await _teamRepository.GetByIdForUpdateAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            var newOwner = await _userRepository.GetByIdAsync(request.NewOwnerUserId, cancellationToken);

            if (newOwner is null || !newOwner.IsActive)
                throw new UserNotFoundException(request.NewOwnerUserId);

            team.TransferOwnership(request.NewOwnerUserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
