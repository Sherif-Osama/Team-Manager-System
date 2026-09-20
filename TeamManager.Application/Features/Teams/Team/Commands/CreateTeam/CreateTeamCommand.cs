using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;

namespace TeamManager.Application.Features.Teams.Team.Commands.CreateTeam
{
    public sealed record CreateTeamCommand(string Name, string? Description) : IRequest<Guid>, IRequiresConfirmedEmail;
}