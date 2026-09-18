using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.CreateTask
{
    public sealed record CreateTaskRequest(string Title, string? Description, Guid? AssigneeUserId, DateOnly? StartDate,
        DateOnly? DueDate, TaskPriority Priority);
}