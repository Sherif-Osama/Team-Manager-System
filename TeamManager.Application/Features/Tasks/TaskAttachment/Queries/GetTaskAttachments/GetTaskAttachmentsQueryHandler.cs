using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Tasks.TaskAttachment.Queries.GetTaskAttachments
{
    public sealed class GetTaskAttachmentsQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
        : IRequestHandler<GetTaskAttachmentsQuery, GetTaskAttachmentsResponse>
    {
        public async Task<GetTaskAttachmentsResponse> Handle(GetTaskAttachmentsQuery request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var query = context.TaskAttachments.AsNoTracking().Where(x => x.TaskId == request.TaskId && x.DeletedAtUtc == null);

            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(x => x.OriginalFileName.Contains(request.Search.Trim()));


            var totalCount = await query.CountAsync(cancellationToken);

            var attachments = await query.OrderByDescending(x => x.UploadedAtUtc).Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize).Select(x =>
                new GetTaskAttachmentsItem(x.Id, x.OriginalFileName, x.ContentType, x.SizeBytes, x.UploadedBy,
                x.UploadedByUser.DisplayName, x.UploadedAtUtc)).ToListAsync(cancellationToken);

            return new GetTaskAttachmentsResponse(attachments, totalCount, request.Page, request.PageSize);
        }
    }
}