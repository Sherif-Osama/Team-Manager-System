namespace TeamManager.Application.Common.Exceptions.ProjectExceptions
{
    public sealed class ProjectNotActiveException : ApplicationExceptionBase
    {
        public ProjectNotActiveException(Guid projectId) : base($"Project '{projectId}' is not active.") { }
    }
}