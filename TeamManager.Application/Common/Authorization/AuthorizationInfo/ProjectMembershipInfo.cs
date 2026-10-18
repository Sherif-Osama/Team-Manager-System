using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Authorization.AuthorizationInfo
{
    public sealed record ProjectMembershipInfo(ProjectRole? Role, bool IsTeamOwner, Guid TeamId);
}
