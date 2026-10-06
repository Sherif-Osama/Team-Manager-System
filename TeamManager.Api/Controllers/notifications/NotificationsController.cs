using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Notifications.Commands.MarkAsRead;
using TeamManager.Application.Features.Notifications.Queries.GetMyNotifications;
using TeamManager.Application.Features.Notifications.Queries.GetUnreadCount;

namespace TeamManager.Api.Controllers.notifications
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController(ISender sender) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<GetMyNotificationsResponse>> GetMyNotifications([FromQuery] GetMyNotificationsQuery query,
            CancellationToken cancellationToken)
        {
            var response = await sender.Send(query, cancellationToken);

            return Ok(response);
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken)
        {
            var count = await sender.Send(new GetUnreadCountQuery(), cancellationToken);

            return Ok(count);
        }

        [HttpPut("{notificationId:long}/read")]
        [Authorize]
        public async Task<IActionResult> MarkAsRead(long notificationId, CancellationToken cancellationToken)
        {
            await sender.Send(new MarkAsReadCommand(notificationId), cancellationToken);

            return NoContent();
        }
    }
}