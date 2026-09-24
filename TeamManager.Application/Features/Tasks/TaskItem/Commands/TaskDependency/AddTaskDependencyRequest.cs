namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.TaskDependency
{
    public sealed record AddTaskDependencyRequest(long DependsOnTaskId);
}