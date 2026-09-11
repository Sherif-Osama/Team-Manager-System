namespace TeamManager.Application.Common.Exceptions
{
    public sealed class ProjectNotFoundException : ApplicationExceptionBase
    {
        public ProjectNotFoundException(Guid projectId) : base($"Project with ID '{projectId}' was not found.") { }
    }
}