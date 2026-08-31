using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.InviteMember
{
    public sealed record InviteMemberRequest(string Email, TeamRole TeamRole);
}