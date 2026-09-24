namespace TeamManager.Application.Common.Exceptions.ProjectExceptions
{
    public sealed class ProjectMemberNotFoundException : ApplicationExceptionBase
    {
        public ProjectMemberNotFoundException(Guid project, long memberId) : base($"The member with id '{memberId}' was not found in project '{project}'.") { }
    }
}