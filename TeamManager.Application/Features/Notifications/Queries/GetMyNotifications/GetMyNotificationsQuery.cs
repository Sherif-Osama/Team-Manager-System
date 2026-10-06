using MediatR;

namespace TeamManager.Application.Features.Notifications.Queries.GetMyNotifications
{
    public sealed record GetMyNotificationsQuery(bool? IsRead, int Page = 1, int PageSize = 20) : IRequest<GetMyNotificationsResponse>;
}