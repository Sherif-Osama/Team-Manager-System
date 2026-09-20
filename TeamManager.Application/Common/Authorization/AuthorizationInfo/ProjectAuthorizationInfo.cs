namespace TeamManager.Application.Common.Authorization.AuthorizationInfo
{
    public sealed record ProjectAuthorizationInfo(bool Exists, bool HasProjectRole, bool IsTeamOwner);
}