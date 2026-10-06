using MediatR;

namespace TeamManager.Application.Features.Notifications.Queries.GetUnreadCount
{
    public sealed record GetUnreadCountQuery : IRequest<int>;
}