using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Notifications.Queries.GetMyNotifications
{
    public sealed record GetMyNotificationsResponse(IReadOnlyCollection<GetMyNotificationsItem> Items, int TotalCount, int Page = 1, int PageSize = 20);

    public sealed record GetMyNotificationsItem(long Id, NotificationType Type, string Title, string? Body,
        string? RelatedEntityType, string? RelatedEntityId, bool IsRead, DateTime? ReadAtUtc, DateTime CreatedAtUtc);
}