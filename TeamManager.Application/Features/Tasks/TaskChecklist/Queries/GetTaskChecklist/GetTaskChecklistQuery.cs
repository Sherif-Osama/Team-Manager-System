using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskChecklist.Queries.GetTaskChecklist
{
    public sealed record GetTaskChecklistQuery(long TaskId, string? Search = null, bool? IsCompleted = null)
        : IRequest<IReadOnlyCollection<GetTaskChecklistResponse>>, ITaskScopedRequest, ICacheableQuery
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Member, ProjectRole.Viewer];

        public string CacheKey => $"teammanager:tasks:checklist:{TaskId}:search:{Search?.Trim() ?? string.Empty}:" +
            $"isCompleted:{IsCompleted}";

        public TimeSpan CacheExpiration => TimeSpan.FromMinutes(10);

        public IReadOnlyCollection<string>? Tags => [CacheTags.TaskChecklist(TaskId)];
    }
}