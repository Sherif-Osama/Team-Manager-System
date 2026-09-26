using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.Project.Commands.ChangeProjectStatus
{
    public sealed record ChangeProjectStatusRequest(ProjectStatus Status);
}