namespace TeamManager.Application.Features.Tasks.TaskAttachment.Queries.GetTaskAttachments
{
    public sealed record GetTaskAttachmentsRequest(string? Search, int Page = 1, int PageSize = 20);
}