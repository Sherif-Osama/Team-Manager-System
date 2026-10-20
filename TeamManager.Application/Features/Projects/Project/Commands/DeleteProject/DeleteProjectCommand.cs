using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.Project.Commands.DeleteProject
{
    public sealed record DeleteProjectCommand(Guid ProjectId) : IRequest, IProjectScopedRequest
    {
        public ProjectRole[] RequiredRoles => [ProjectRole.Owner];
    }
}
