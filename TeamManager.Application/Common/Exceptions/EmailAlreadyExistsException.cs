namespace TeamManager.Application.Common.Exceptions
{
    public sealed class EmailAlreadyExistsException : ApplicationExceptionBase
    {
        public EmailAlreadyExistsException(string email) : base($"The email '{email}' is already registered.") { }
    }
}