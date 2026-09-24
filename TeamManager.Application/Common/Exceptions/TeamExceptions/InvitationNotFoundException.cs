namespace TeamManager.Application.Common.Exceptions.TeamExceptions
{
    public sealed class InvitationNotFoundException : ApplicationExceptionBase
    {
        public InvitationNotFoundException() : base("The invitation was not found.") { }
    }
}