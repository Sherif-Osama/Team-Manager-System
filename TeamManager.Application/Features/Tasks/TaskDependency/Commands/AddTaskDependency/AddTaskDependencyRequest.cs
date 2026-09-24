namespace TeamManager.Application.Features.Tasks.TaskDependency.Commands.TaskDependency
{
    public sealed record AddTaskDependencyRequest(long DependsOnTaskId);
}