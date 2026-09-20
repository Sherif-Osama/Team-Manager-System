using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Authorization.Scopes
{
    public interface ITeamScopedRequest
    {
        Guid TeamId { get; }

        TeamRole[] RequiredRoles { get; }
    }
}