using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Team.Commands.UpdateTeam
{
    public sealed class UpdateTeamCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateTeamCommand>
    {
        public async Task Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
        {
            var team = await teamRepository.GetByIdForUpdateAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            var existingTeam = await teamRepository.GetByNameAsync(request.Name, cancellationToken);

            if (existingTeam is not null && existingTeam.Id != team.Id)
                throw new TeamNameAlreadyExistsException(request.Name);

            team.Rename(request.Name);
            team.UpdateDescription(request.Description);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}