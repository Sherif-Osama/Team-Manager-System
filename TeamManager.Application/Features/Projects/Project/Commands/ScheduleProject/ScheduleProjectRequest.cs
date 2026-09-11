namespace TeamManager.Application.Features.Projects.Project.Commands.ScheduleProject
{
    public sealed record ScheduleProjectRequest(DateOnly? StartDate, DateOnly? DueDate);
}