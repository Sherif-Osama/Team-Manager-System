using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.Project.Commands.ChangeProjectStatus
{
    public sealed record ChangeProjectStatusCommand(Guid ProjectId, ProjectStatus Status) : IRequest, IProjectScopedRequest
    {
        public ProjectRole[] RequiredRoles => [ProjectRole.Owner];
    }
}