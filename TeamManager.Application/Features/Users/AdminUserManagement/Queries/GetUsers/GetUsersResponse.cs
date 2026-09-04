namespace TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUsers
{
    public sealed record GetUsersResponse(IReadOnlyList<UserListItem> Users, int Page, int PageSize,
        int TotalCount);

    public sealed record UserListItem(Guid Id, string Email, string DisplayName, bool IsEmailConfirmed,
        bool IsActive, DateTime CreatedAtUtc, DateTime? LastLoginUtc, IReadOnlyCollection<string> SystemRolesNames);
};