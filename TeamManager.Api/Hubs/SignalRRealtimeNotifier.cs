using Microsoft.AspNetCore.SignalR;
using TeamManager.Application.Abstractions.Realtime;
using TeamManager.Application.Features.Notifications.Realtime;

namespace TeamManager.Api.Hubs
{
    public sealed class SignalRRealtimeNotifier(IHubContext<NotificationsHub> hubContext) : IRealtimeNotifier
    {
        public async Task NotifyUserAsync(Guid userId, NotificationRealtimePayload payload, CancellationToken cancellationToken)
        {
            await hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", payload, cancellationToken);
        }
    }
}