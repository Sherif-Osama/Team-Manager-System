namespace TeamManager.Application.Common.Exceptions.TaskExceptions
{
    public sealed class CommentNotFoundException : ApplicationExceptionBase
    {
        public CommentNotFoundException() : base("Comment not found.") { }
    }
}