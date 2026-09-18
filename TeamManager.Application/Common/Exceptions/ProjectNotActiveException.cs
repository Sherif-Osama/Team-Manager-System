namespace TeamManager.Application.Common.Exceptions
{
    public sealed class ProjectNotActiveException : ApplicationExceptionBase
    {
        public ProjectNotActiveException(Guid projectId) : base($"Project '{projectId}' is not active.") { }
    }
}