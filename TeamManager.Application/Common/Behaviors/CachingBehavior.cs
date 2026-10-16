using MediatR;
using TeamManager.Application.Abstractions.Caching;

namespace TeamManager.Application.Common.Behaviors
{
    public sealed class CachingBehavior<TRequest, TResponse>(ICacheService cacheService) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICacheableQuery
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {

            var response = await cacheService.GetOrCreateAsync(request.CacheKey, ct => next(ct), request.CacheExpiration,
                request.Tags, cancellationToken);

            return response;
        }
    }
}