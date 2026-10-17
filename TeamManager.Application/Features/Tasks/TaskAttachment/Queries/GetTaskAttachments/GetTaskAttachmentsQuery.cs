using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskAttachment.Queries.GetTaskAttachments
{
    public sealed record GetTaskAttachmentsQuery(long TaskId, string? Search, int Page = 1, int PageSize = 20)
        : IRequest<GetTaskAttachmentsResponse>, ITaskScopedRequest, ICacheableQuery
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Member, ProjectRole.Viewer];
        public string CacheKey => $"teammanager:task:{TaskId}:attachments" + $":search:{Search?.Trim() ?? string.Empty}" +
        $":page:{Page}" + $":pageSize:{PageSize}";

        public TimeSpan CacheExpiration => TimeSpan.FromMinutes(10);

        public IReadOnlyCollection<string>? Tags =>
        [
            CacheTags.Task(TaskId),
            CacheTags.TaskAttachments(TaskId)
        ];
    }
}