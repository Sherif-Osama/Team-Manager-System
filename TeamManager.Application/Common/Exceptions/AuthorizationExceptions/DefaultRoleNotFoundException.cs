namespace TeamManager.Application.Common.Exceptions.AuthorizationExceptions
{
    public sealed class DefaultRoleNotFoundException : ApplicationExceptionBase
    {
        public DefaultRoleNotFoundException(string message) : base(message)
        { }
    }
}