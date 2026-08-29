using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Team.Commands.ActivateTeam
{
    public sealed class ActivateTeamCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<ActivateTeamCommand>
    {
        public async Task Handle(ActivateTeamCommand request, CancellationToken cancellationToken)
        {
            var team = await teamRepository.GetByIdForUpdateAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            team.Activate();

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}