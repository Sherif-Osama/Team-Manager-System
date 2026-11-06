namespace TeamManager.IntegrationTest.Users.Configuration
{
    public sealed record GetMyProfileResponseDto(Guid Id, string Email, string DisplayName, bool IsEmailConfirmed,
        bool IsActive, DateTime? LastLoginUtc, DateTime CreatedAtUtc, int ActiveTeamMembersCount, int OwnedTeamsCount,
        IReadOnlyCollection<string> SystemRolesNames);
}
