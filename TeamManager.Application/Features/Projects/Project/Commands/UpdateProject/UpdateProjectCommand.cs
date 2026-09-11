using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.Project.Commands.UpdateProject
{
    public sealed record UpdateProjectCommand(Guid ProjectId, string Name, string? Description) : IRequest, IProjectScopedRequest
    {
        public ProjectRole[] RequiredRoles => [ProjectRole.Owner];
    }
}