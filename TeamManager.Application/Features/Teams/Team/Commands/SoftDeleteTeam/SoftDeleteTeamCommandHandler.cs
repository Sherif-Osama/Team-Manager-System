using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Team.Commands.SoftDeleteTeam
{
    public sealed class SoftDeleteTeamCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<SoftDeleteTeamCommand>
    {

        public async Task Handle(SoftDeleteTeamCommand request, CancellationToken cancellationToken)
        {
            var team = await teamRepository.GetByIdForUpdateAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            team.SoftDelete();

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}