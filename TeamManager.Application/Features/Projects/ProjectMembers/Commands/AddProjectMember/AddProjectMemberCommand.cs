using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.AddProjectMember
{
    public sealed record AddProjectMemberCommand(Guid ProjectId, Guid UserId, ProjectRole ProjectRole)
        : IRequest, IProjectScopedRequest
    {
        public ProjectRole[] RequiredRoles => [ProjectRole.Owner, ProjectRole.Admin];
    }
}