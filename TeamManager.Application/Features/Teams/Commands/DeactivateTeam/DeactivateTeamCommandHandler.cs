using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Commands.DeactivateTeam
{
    public sealed class DeactivateTeamCommandHandler : IRequestHandler<DeactivateTeamCommand>
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeactivateTeamCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
        {
            _teamRepository = teamRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeactivateTeamCommand request, CancellationToken cancellationToken)
        {
            var team = await _teamRepository.GetByIdForUpdateAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            team.Deactivate();

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}