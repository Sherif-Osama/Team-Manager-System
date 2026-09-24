namespace TeamManager.Application.Features.Tasks.TaskDependency.Queries.GetTaskDependencies
{
    public sealed record GetTaskDependenciesRequest(int Page = 1, int PageSize = 20);
}