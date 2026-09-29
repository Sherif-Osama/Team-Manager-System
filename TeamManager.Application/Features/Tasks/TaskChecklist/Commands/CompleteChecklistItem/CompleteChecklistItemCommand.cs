using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskChecklist.Commands.CompleteChecklistItem
{
    public sealed record CompleteChecklistItemCommand(long TaskId, long ChecklistItemId) : IRequest, ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Member];
    }
}