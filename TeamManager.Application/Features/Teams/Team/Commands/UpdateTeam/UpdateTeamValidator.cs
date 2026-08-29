using FluentValidation;

namespace TeamManager.Application.Features.Teams.Team.Commands.UpdateTeam
{
    public sealed class UpdateTeamValidator : AbstractValidator<UpdateTeamCommand>
    {
        public UpdateTeamValidator()
        {

            RuleFor(C => C.TeamId).NotEmpty().NotNull();
            RuleFor(C => C.Name).Must(name => !string.IsNullOrWhiteSpace(name)).MaximumLength(100);
            RuleFor(C => C.Description).MaximumLength(500);
        }
    }
}