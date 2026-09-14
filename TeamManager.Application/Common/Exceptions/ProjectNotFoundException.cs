namespace TeamManager.Application.Common.Exceptions
{
    public sealed class ProjectNotFoundException : ApplicationExceptionBase
    {
        public ProjectNotFoundException(Guid projectId) : base($"Project with ID '{projectId}' was not found.") { }
        public ProjectNotFoundException(string projectName) : base($"Project with name '{projectName}' was not found.") { }
    }
}