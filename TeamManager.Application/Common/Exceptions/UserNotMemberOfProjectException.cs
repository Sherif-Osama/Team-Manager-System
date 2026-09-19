namespace TeamManager.Application.Common.Exceptions
{
    public sealed class UserNotMemberOfProjectException : ApplicationExceptionBase
    {
        public UserNotMemberOfProjectException(Guid userId, Guid projectId) :
            base($"User '{userId}' is not an active member of project '{projectId}'.")
        { }
    }
}