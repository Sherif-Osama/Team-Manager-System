using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskComment.Queries.GetTaskComments
{
    public sealed record GetTaskCommentsQuery(long TaskId, int Page = 1, int PageSize = 20) : IRequest<GetTaskCommentsResponse>,
        ITaskScopedRequest, ICacheableQuery
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Member, ProjectRole.Viewer];

        public string CacheKey => $"teammanager:task:{TaskId}:comments" + $":page:{Page}" + $":pageSize:{PageSize}";

        public TimeSpan CacheExpiration => TimeSpan.FromMinutes(10);

        public IReadOnlyCollection<string>? Tags =>
        [
            CacheTags.Task(TaskId),
            CacheTags.TaskComments(TaskId)
        ];
    }
}