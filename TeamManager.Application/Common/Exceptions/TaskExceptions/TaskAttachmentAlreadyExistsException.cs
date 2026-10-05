namespace TeamManager.Application.Common.Exceptions.TaskExceptions
{
    public sealed class TaskAttachmentAlreadyExistsException : ApplicationExceptionBase
    {
        public TaskAttachmentAlreadyExistsException() : base("This file is already attached to the task.")
        { }
    }
}