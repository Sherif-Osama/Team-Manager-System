namespace TeamManager.Application.Common.Exceptions.ProjectExceptions
{
    public sealed class ProjectHasActiveTasksException : ApplicationExceptionBase
    {
        public ProjectHasActiveTasksException(Guid projectId) : base($"Project with id '{projectId}' has active tasks and cannot be deleted.") { }
    }
}
