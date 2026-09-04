namespace TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserByEmail
{
    public sealed record GetUserByEmailResponse(Guid Id, string Email, string DisplayName, bool IsEmailConfirmed,
        bool IsActive, DateTime? LastLoginUtc, DateTime CreatedAtUtc, int ActiveTeamsCount, int OwnedTeamsCount, IReadOnlyCollection<string> SystemRolesNames);
}