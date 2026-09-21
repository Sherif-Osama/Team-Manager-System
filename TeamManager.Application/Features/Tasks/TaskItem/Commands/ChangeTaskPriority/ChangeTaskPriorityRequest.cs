using TeamManager.Domain.Enums;
namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.ChangeTaskPriority
{
    public sealed record ChangeTaskPriorityRequest(TaskPriority Priority);
}