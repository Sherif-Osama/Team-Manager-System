namespace TeamManager.Application.Common.Exceptions
{
    public sealed class TeamMemberNotFoundException : ApplicationExceptionBase
    {
        public TeamMemberNotFoundException(Guid teamId, long memberId) :
            base($"The member with id '{memberId}' was not found in team '{teamId}'.")
        { }
    }
}
