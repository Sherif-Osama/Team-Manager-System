using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Authorization.Scopes
{
    public interface IProjectScopedRequest
    {
        Guid ProjectId { get; }
        ProjectRole[] RequiredRoles { get; }
    }
}