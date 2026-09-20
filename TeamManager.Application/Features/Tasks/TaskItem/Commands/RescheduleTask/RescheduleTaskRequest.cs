namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.RescheduleTask
{
    public sealed record RescheduleTaskRequest(DateOnly? StartDate, DateOnly? DueDate);
}