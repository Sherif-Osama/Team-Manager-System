using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Authorization;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Common.Behaviors
{
    public sealed class PermissionAuthorizationBehavior<TRequest, TResponse>(ICurrentUser currentUser, IUserRepository userRepository)
        : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequiresPermission
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var hasPermission = await userRepository.HasPermissionAsync(currentUser.UserId.Value, request.PermissionCode,
                cancellationToken);

            if (!hasPermission)
                throw new ForbiddenException("You do not have permission to perform this action.");

            return await next();
        }
    }
}