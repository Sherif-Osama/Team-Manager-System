using FluentValidation;

namespace TeamManager.Application.Features.Teams.Commands.SoftDeleteTeam
{
    public sealed class SoftDeleteTeamValidator : AbstractValidator<SoftDeleteTeamCommand>
    {
        public SoftDeleteTeamValidator() { RuleFor(C => C.TeamId).NotEmpty(); }
    }
}
