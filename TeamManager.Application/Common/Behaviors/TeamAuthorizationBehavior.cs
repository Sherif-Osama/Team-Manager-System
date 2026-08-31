using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Authorization;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Common.Behaviors
{
    public sealed class TeamAuthorizationBehavior<TRequest, TResponse>(ITeamRepository teamRepository, ICurrentUser currentUser)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ITeamScopedRequest
    {

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue)
                throw new ForbiddenException("You do not have permission to perform this action on this team.");

            var isAuthorized = await teamRepository.HasActiveRoleAsync(request.TeamId, currentUser.UserId.Value,
                request.RequiredRoles, cancellationToken);

            if (!isAuthorized)
                throw new ForbiddenException("You do not have permission to perform this action on this team.");

            return await next();
        }
    }
}