using FluentValidation;

namespace TeamManager.Application.Features.Teams.Team.Commands.DeactivateTeam
{
    public sealed class DeactivateTeamValidator : AbstractValidator<DeactivateTeamCommand>
    {
        public DeactivateTeamValidator()
        {
            RuleFor(C => C.TeamId).NotEmpty();
        }
    }
}
