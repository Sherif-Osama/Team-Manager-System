using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Storage;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskAttachment.Queries.DownloadTaskAttachment
{
    public sealed class DownloadTaskAttachmentQueryHandler(IApplicationDbContext context, IFileStorage fileStorage)
        : IRequestHandler<DownloadTaskAttachmentQuery, DownloadTaskAttachmentResponse>
    {
        public async Task<DownloadTaskAttachmentResponse> Handle(DownloadTaskAttachmentQuery request, CancellationToken cancellationToken)
        {
            var attachment = await context.TaskAttachments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.AttachmentId
            && x.TaskId == request.TaskId && x.Task.DeletedAtUtc == null, cancellationToken);

            if (attachment is null)
                throw new TaskAttachmentNotFoundException(request.TaskId, request.AttachmentId);

            var content = await fileStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);

            return new DownloadTaskAttachmentResponse(content, attachment.ContentType, attachment.OriginalFileName);
        }
    }
}