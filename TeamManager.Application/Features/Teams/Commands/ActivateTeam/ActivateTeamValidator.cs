using FluentValidation;

namespace TeamManager.Application.Features.Teams.Commands.ActivateTeam
{
    public sealed class ActivateTeamValidator : AbstractValidator<ActivateTeamCommand>
    {
        public ActivateTeamValidator()
        {
            RuleFor(C => C.TeamId).NotEmpty();
        }
    }
}