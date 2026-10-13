using TeamManager.Domain.Enums;

namespace TeamManager.Application.Abstractions.Scopes
{
    public interface IProjectScopedRequest
    {
        Guid ProjectId { get; }
        ProjectRole[] RequiredRoles { get; }
    }
}