namespace TeamManager.Application.Common.Exceptions.TaskExceptions
{
    public sealed class TaskAttachmentNotFoundException : ApplicationExceptionBase
    {
        public TaskAttachmentNotFoundException(long taskId, long attachmentId)
            : base($"Task attachment '{attachmentId}' was not found for task '{taskId}'.") { }
    }
}