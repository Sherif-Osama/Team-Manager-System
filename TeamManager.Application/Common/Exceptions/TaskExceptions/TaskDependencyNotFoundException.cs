namespace TeamManager.Application.Common.Exceptions.TaskExceptions
{
    public sealed class TaskDependencyNotFoundException : ApplicationExceptionBase
    {
        public TaskDependencyNotFoundException(long taskId, long dependencyId)
            : base($"Task dependency '{dependencyId}' was not found for task '{taskId}'.") { }
    }
}