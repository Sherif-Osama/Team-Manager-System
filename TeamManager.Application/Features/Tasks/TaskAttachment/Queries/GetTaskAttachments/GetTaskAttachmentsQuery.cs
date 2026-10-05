using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskAttachment.Queries.GetTaskAttachments
{
    public sealed record GetTaskAttachmentsQuery(long TaskId, string? Search, int Page = 1, int PageSize = 20)
        : IRequest<GetTaskAttachmentsResponse>, ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Member, ProjectRole.Viewer];
    }
}