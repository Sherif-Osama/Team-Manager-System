using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Caching;
using TeamManager.Application.Common.Exceptions.TeamExceptions;

namespace TeamManager.Application.Features.Teams.Team.Commands.SoftDeleteTeam
{
    public sealed class SoftDeleteTeamCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork,
        IProjectRepository projectRepository, ICacheService cacheService)
        : IRequestHandler<SoftDeleteTeamCommand>
    {

        public async Task Handle(SoftDeleteTeamCommand request, CancellationToken cancellationToken)
        {
            var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            var hasActiveProjects = await projectRepository.HasActiveProjectsInTeamAsync(request.TeamId, cancellationToken);

            if (hasActiveProjects)
                throw new TeamHasActiveProjectsException(request.TeamId);

            team.SoftDelete();

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // SoftDelete raises no domain events, so the post-commit dispatcher would
            // never run; invalidate cached team roles directly after the commit.
            await cacheService.RemoveByTagAsync(CacheTags.TeamAuthorization(request.TeamId), cancellationToken);
        }
    }
}
