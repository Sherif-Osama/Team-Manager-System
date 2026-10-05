namespace TeamManager.Application.Features.Tasks.TaskAttachment.Queries.GetTaskAttachments
{
    public sealed record GetTaskAttachmentsResponse(IReadOnlyCollection<GetTaskAttachmentsItem> Items, int TotalCount,
        int Page = 1, int PageSize = 20);

    public sealed record GetTaskAttachmentsItem(long Id, string OriginalFileName, string ContentType,
        long SizeBytes, Guid UploadedByUserId, string UploadedByUserName, DateTime UploadedAtUtc);
}