namespace TeamManager.Application.Features.Projects.Project.Commands.CreateProject
{
    public sealed record CreateProjectRequest(string Name, string? Description, DateOnly? startDate = null, DateOnly? dueDate = null);
}