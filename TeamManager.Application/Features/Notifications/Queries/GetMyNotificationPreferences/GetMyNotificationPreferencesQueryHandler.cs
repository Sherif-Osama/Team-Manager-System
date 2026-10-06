using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Notifications.Queries.GetMyNotificationPreferences
{
    public sealed class GetMyNotificationPreferencesQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
        : IRequestHandler<GetMyNotificationPreferencesQuery, GetMyNotificationPreferencesResponse>
    {
        public async Task<GetMyNotificationPreferencesResponse> Handle(GetMyNotificationPreferencesQuery request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            var preferences = await context.NotificationPreferences.AsNoTracking().Where(x => x.UserId == userId
            && x.User.DeletedAtUtc == null).ToDictionaryAsync(x => x.NotificationType, x => x.IsEnabled, cancellationToken);

            var items = Enum.GetValues<NotificationType>().Select(type => new GetMyNotificationPreferenceItem(type,
                preferences.GetValueOrDefault(type, true))).ToList();

            return new GetMyNotificationPreferencesResponse(items);
        }
    }
}