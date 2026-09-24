namespace TeamManager.Application.Common.Exceptions.ProjectExceptions
{
    public sealed class UserOwnsActiveProjectException : ApplicationExceptionBase
    {
        public UserOwnsActiveProjectException(Guid UserId) : base($"user with ID '{UserId}' owns an active project.") { }
    }
}