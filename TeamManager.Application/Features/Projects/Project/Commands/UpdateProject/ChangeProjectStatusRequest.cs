using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.Project.Commands.UpdateProject
{
    public sealed record ChangeProjectStatusRequest(ProjectStatus Status);
}