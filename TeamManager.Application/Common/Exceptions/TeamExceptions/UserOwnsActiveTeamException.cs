namespace TeamManager.Application.Common.Exceptions.TeamExceptions
{
    public sealed class UserOwnsActiveTeamException : ApplicationExceptionBase
    {
        public UserOwnsActiveTeamException(Guid UserId) : base($"user with ID '{UserId}' owns an active team.") { }
    }
}