using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskAttachment.Commands.AddTaskAttachment
{
    public sealed record AddTaskAttachmentCommand(long TaskId, string FileName, string ContentType, long SizeBytes, Stream Content)
        : IRequest<long>, ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Member];
    }
}