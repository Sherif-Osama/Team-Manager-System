namespace TeamManager.Application.Common.Exceptions.UserExceptions
{
    public sealed class UserNotFoundException : ApplicationExceptionBase
    {
        public UserNotFoundException(Guid userId) : base($"The user with id '{userId}' was not found.") { }
        public UserNotFoundException(string email) : base($"The user with email '{email}' was not found.") { }
    }
}
