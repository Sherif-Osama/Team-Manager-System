using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Notifications.Queries.GetUnreadCount
{
    public sealed class GetUnreadCountQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
        : IRequestHandler<GetUnreadCountQuery, int>
    {
        public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            return await context.Notifications.AsNoTracking().CountAsync(x => x.RecipientUserId == userId && !x.IsRead,
                cancellationToken);
        }
    }
}