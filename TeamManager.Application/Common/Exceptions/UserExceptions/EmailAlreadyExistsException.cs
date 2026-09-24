namespace TeamManager.Application.Common.Exceptions.UserExceptions
{
    public sealed class EmailAlreadyExistsException : ApplicationExceptionBase
    {
        public EmailAlreadyExistsException(string email) : base($"The email '{email}' is already registered.") { }
    }
}