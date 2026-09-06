namespace TeamManager.Application.Common.Exceptions
{
    public sealed class DefaultRoleNotFoundException : ApplicationExceptionBase
    {
        public DefaultRoleNotFoundException(string message) : base(message)
        { }
    }
}