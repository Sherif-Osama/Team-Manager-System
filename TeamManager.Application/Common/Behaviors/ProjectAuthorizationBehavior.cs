using MediatR;
using TeamManager.Application.Abstractions;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Authorization;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Domain.Enums;
namespace TeamManager.Application.Common.Behaviors
{
    public class ProjectAuthorizationBehavior<TRequest, TResponse>(ICurrentUser currentUser,
        IProjectRepository projectRepository, ITeamRepository teamRepository) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IProjectScopedRequest
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue || !currentUser.IsAuthenticated)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

            if (project is null)
                throw new ProjectNotFoundException(request.ProjectId);

            var hasProjectRole = await projectRepository.HasActiveRoleAsync(request.ProjectId, currentUser.UserId.Value,
                request.RequiredRoles, cancellationToken);

            if (hasProjectRole)
                return await next();

            var isTeamOwner = await teamRepository.HasActiveRoleAsync(project.TeamId, currentUser.UserId.Value, [TeamRole.Owner],
                cancellationToken);

            if (!isTeamOwner)
                throw new ForbiddenException("You do not have permission to perform this action on this project.");

            return await next();
        }
    }
}