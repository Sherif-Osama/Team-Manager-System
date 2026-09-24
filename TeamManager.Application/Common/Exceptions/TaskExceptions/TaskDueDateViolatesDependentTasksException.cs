namespace TeamManager.Application.Common.Exceptions.TaskExceptions
{
    public sealed class TaskDueDateViolatesDependentTasksException : Exception
    {
        public TaskDueDateViolatesDependentTasksException(long taskId, IReadOnlyCollection<(string Title, DateOnly dueDate)> violatingTasks)
            : base($"Task '{taskId}' cannot be rescheduled because its new due date would be after the due date of the following dependent task(s): " +
            $"{string.Join(", ", violatingTasks.Select(x => $"'{x.Title}' (due: {x.dueDate:yyyy-MM-dd})"))}.")
        { }
    }
}