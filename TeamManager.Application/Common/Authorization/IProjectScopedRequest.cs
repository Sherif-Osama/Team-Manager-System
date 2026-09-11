using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Authorization
{
    public interface IProjectScopedRequest
    {
        Guid ProjectId { get; }
        TeamRole[] RequiredRoles { get; }
    }
}