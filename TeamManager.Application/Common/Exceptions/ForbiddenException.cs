namespace TeamManager.Application.Common.Exceptions
{
    public sealed class ForbiddenException : ApplicationExceptionBase
    {
        public ForbiddenException(string message) : base(message) { }
    }
}
