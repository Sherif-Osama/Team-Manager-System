using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;
namespace TeamManager.Application.Features.Tasks.TaskChecklist.Commands.AddChecklistItem
{
    public sealed record AddChecklistItemCommand(long TaskId, string Content) : IRequest<long>, ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Member];
    }
}