using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Authorization
{
    public interface ITeamScopedRequest
    {
        Guid TeamId { get; }

        TeamRole[] RequiredRoles { get; }
    }
}