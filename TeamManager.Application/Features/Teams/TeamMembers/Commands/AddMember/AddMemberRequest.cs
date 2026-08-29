using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.AddMember
{
    public sealed record AddMemberRequest(Guid UserId, TeamRole TeamRole);
}