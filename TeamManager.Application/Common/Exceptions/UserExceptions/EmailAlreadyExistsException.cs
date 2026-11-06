namespace TeamManager.Application.Common.Exceptions.UserExceptions
{
    public sealed class EmailAlreadyExistsException : ApplicationExceptionBase
    {
        public EmailAlreadyExistsException(string email) : base($"Invalid email address.") { }
    }
}