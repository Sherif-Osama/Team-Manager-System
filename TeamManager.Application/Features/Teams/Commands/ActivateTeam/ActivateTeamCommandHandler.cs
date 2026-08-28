using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Commands.ActivateTeam
{
    public sealed class ActivateTeamCommandHandler
        : IRequestHandler<ActivateTeamCommand>
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ActivateTeamCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
        {
            _teamRepository = teamRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(ActivateTeamCommand request, CancellationToken cancellationToken)
        {
            var team = await _teamRepository.GetByIdForUpdateAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            team.Activate();

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
