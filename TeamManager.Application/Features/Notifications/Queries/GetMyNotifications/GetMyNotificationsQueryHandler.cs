using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Notifications.Queries.GetMyNotifications
{
    public sealed class GetMyNotificationsQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
        : IRequestHandler<GetMyNotificationsQuery, GetMyNotificationsResponse>
    {
        public async Task<GetMyNotificationsResponse> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId!.Value;

            var query = context.Notifications.AsNoTracking().Where(x => x.RecipientUserId == userId);

            if (request.IsRead.HasValue)
                query = query.Where(x => x.IsRead == request.IsRead.Value);


            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query.OrderByDescending(x => x.CreatedAtUtc).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
                .Select(x => new GetMyNotificationsItem(x.Id, x.Type, x.Title, x.Body, x.RelatedEntityType, x.RelatedEntityId,
                x.IsRead, x.ReadAtUtc, x.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetMyNotificationsResponse(items, totalCount, request.Page, request.PageSize);
        }
    }
}