using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Authorization;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Common.Behaviors
{
    public sealed class ConfirmedEmailBehavior<TRequest, TResponse>(ICurrentUser currentUser, IUserRepository userRepository)
        : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequiresConfirmedEmail
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken);

            if (user is null)
                throw new UnauthorizedAccessException("User is not authenticated.");

            if (!user.IsEmailConfirmed)
                throw new ForbiddenException("You must confirm your email address before performing this action.");

            return await next();
        }
    }
}
