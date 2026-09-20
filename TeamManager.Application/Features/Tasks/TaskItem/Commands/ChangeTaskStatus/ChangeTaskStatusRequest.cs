using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.ChangeTaskStatus
{
    public sealed record ChangeTaskStatusRequest(TaskItemStatus Status);
}