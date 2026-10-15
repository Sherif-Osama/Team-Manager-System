namespace TeamManager.Application.Common.Exceptions.AuthorizationExceptions
{
    public sealed class InvalidTokenException : ApplicationExceptionBase
    {
        public InvalidTokenException(string message) : base(message) { }
    }
}