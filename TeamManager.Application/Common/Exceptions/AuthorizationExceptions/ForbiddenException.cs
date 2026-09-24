namespace TeamManager.Application.Common.Exceptions.AuthorizationExceptions
{
    public sealed class ForbiddenException : ApplicationExceptionBase
    {
        public ForbiddenException(string message) : base(message) { }
    }
}
