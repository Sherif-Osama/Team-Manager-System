namespace TeamManager.Application.Common.Exceptions.TaskExceptions
{
    public sealed class TaskDateConflictException : ApplicationExceptionBase
    {
        public TaskDateConflictException(IReadOnlyList<string> conflictingTaskTitles)
            :
            base($"Cannot reschedule the project: the following task(s) fall outside the new date range: " + $"{string.Join(", ", conflictingTaskTitles)}. Reschedule or remove them first.")
        { }
    }
}