namespace TeamManager.Application.Features.Tasks.TaskAttachment.Queries.DownloadTaskAttachment
{
    public sealed record DownloadTaskAttachmentResponse(Stream Content, string ContentType, string FileName);
}