using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TeamManager.Api.Hubs
{
    [Authorize]
    public sealed class NotificationsHub : Hub { }
}