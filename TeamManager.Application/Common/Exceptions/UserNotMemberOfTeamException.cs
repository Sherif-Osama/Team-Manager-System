namespace TeamManager.Application.Common.Exceptions
{
    public sealed class UserNotMemberOfTeamException : ApplicationExceptionBase
    {
        public UserNotMemberOfTeamException(Guid teamId, Guid userId) :
            base($"User '{userId}' is not an active member of team '{teamId}'.")
        { }
    }
}