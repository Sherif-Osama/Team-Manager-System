using MediatR;

namespace TeamManager.Application.Features.Notifications.Commands.MarkAsRead
{
    public sealed record MarkAsReadCommand(long NotificationId) : IRequest;
}