namespace TeamManager.Application.Common.Exceptions
{
    public sealed class TaskHasIncompleteDependenciesException : ApplicationExceptionBase
    {
        public TaskHasIncompleteDependenciesException(long taskId, IReadOnlyCollection<string> dependencyTitles)
            : base($"Task '{taskId}' cannot be completed because the following dependencies are not done: " + $"{string.Join(", ", dependencyTitles)}.") { }
    }
}