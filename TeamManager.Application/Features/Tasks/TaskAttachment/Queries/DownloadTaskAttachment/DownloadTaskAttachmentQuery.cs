using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskAttachment.Queries.DownloadTaskAttachment
{
    public sealed record DownloadTaskAttachmentQuery(long TaskId, long AttachmentId)
        : IRequest<DownloadTaskAttachmentResponse>, ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Member, ProjectRole.Viewer];
    }
}