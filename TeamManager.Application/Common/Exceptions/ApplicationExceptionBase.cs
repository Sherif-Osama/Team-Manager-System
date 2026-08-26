namespace TeamManager.Application.Common.Exceptions
{
    public class ApplicationExceptionBase : Exception
    {
        public ApplicationExceptionBase(string message) : base(message) { }
    }
}