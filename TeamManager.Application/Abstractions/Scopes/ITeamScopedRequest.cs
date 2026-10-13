using TeamManager.Domain.Enums;

namespace TeamManager.Application.Abstractions.Scopes
{
    public interface ITeamScopedRequest
    {
        Guid TeamId { get; }

        TeamRole[] RequiredRoles { get; }
    }
}