namespace TeamManager.Application.Common.Exceptions
{
    public sealed class InvitationNotFoundException : Exception
    {
        public InvitationNotFoundException() : base("The invitation was not found.") { }
    }
}