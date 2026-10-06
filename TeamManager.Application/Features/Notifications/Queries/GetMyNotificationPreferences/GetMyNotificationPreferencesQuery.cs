using MediatR;

namespace TeamManager.Application.Features.Notifications.Queries.GetMyNotificationPreferences
{
    public sealed record GetMyNotificationPreferencesQuery : IRequest<GetMyNotificationPreferencesResponse>;
}