using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.ChangeMemberRole
{
    public sealed record ChangeMemberRoleCommand(Guid ProjectId, long ProjectMemberId, ProjectRole Role) : IRequest,
        IProjectScopedRequest
    {
        public ProjectRole[] RequiredRoles => [ProjectRole.Owner, ProjectRole.Admin];
    }
}