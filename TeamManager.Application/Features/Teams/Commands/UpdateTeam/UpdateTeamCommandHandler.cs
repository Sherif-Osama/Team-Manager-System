using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Commands.UpdateTeam
{
    public sealed class UpdateTeamCommandHandler : IRequestHandler<UpdateTeamCommand>
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTeamCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
        {
            _teamRepository = teamRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
        {
            var team = await _teamRepository.GetByIdForUpdateAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            var existingTeam = await _teamRepository.GetByNameAsync(request.Name, cancellationToken);

            if (existingTeam is not null && existingTeam.Id != team.Id)
                throw new TeamNameAlreadyExistsException(request.Name);

            team.Rename(request.Name);
            team.UpdateDescription(request.Description);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}