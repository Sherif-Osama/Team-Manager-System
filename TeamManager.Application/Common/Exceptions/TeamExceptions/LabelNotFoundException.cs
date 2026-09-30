namespace TeamManager.Application.Common.Exceptions.TeamExceptions
{
    public sealed class LabelNotFoundException : ApplicationExceptionBase
    {
        public LabelNotFoundException(long labelId) : base($"Label with id '{labelId}' was not found.") { }
    }
}