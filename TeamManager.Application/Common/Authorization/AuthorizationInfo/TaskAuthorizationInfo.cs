namespace TeamManager.Application.Common.Authorization.AuthorizationInfo
{
    public sealed record TaskAuthorizationInfo(bool Exists, bool HasProjectRole, bool IsTeamOwner);
}