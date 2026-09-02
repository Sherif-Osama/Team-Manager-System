namespace TeamManager.Application.Features.Users.Queries.GetUserByEmail
{
    public sealed record GetUserByEmailResponse(Guid Id, string Email, string DisplayName, bool IsEmailConfirmed,
        bool IsActive, DateTime? LastLoginUtc, DateTime CreatedAtUtc, int ActiveTeamsCount, int OwnedTeamsCount);
}