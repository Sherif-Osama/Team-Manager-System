using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Authorization
{
    public interface ITaskScopedRequest
    {
        long TaskId { get; }
        ProjectRole[] RequiredProjectRoles { get; }
    }
}