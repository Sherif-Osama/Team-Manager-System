using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Team.Commands.DeactivateTeam
{
    public sealed class DeactivateTeamCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<DeactivateTeamCommand>
    {

        public async Task Handle(DeactivateTeamCommand request, CancellationToken cancellationToken)
        {
            var team = await teamRepository.GetByIdForUpdateAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            team.Deactivate();

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}