using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.RemoveMember
{
    public sealed record RemoveMemberCommand(Guid ProjectId, long MemberId) : IRequest, IProjectScopedRequest
    {
        public ProjectRole[] RequiredRoles => [ProjectRole.Owner, ProjectRole.Admin];
    }
}