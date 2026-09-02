namespace TeamManager.Application.Common.Exceptions
{
    public sealed class InvitationNotFoundException : ApplicationExceptionBase
    {
        public InvitationNotFoundException() : base("The invitation was not found.") { }
    }
}