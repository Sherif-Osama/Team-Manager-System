using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.AddProjectMember
{
    public sealed record AddProjectMemberRequest(Guid UserId, ProjectRole ProjectRole);
}