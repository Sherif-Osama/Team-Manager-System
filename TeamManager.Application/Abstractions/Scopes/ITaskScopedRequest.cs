using TeamManager.Domain.Enums;

namespace TeamManager.Application.Abstractions.Scopes
{
    public interface ITaskScopedRequest
    {
        long TaskId { get; }
        ProjectRole[] RequiredProjectRoles { get; }
    }
}