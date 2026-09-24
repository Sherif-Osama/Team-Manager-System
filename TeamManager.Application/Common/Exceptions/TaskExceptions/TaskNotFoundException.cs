namespace TeamManager.Application.Common.Exceptions.TaskExceptions
{
    public sealed class TaskNotFoundException : ApplicationExceptionBase
    {
        public TaskNotFoundException(long taskId) : base($"The task with id '{taskId}' was not found.") { }
    }
}