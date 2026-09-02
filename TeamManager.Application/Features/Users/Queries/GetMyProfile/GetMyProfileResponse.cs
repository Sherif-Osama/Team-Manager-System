namespace TeamManager.Application.Features.Users.Queries.GetMyProfile
{
    public sealed record GetMyProfileResponse(Guid Id, string Email, string DisplayName, bool IsEmailConfirmed, bool IsActive,
        DateTime? LastLoginUtc, DateTime CreatedAtUtc);
}