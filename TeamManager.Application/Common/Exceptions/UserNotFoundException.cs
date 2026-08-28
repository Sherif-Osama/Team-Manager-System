namespace TeamManager.Application.Common.Exceptions
{
    public sealed class UserNotFoundException : ApplicationExceptionBase
    {
        public UserNotFoundException(Guid userId) : base($"The user with id '{userId}' was not found.") { }
    }
}
