using FluentValidation;

namespace TeamManager.Application.Features.Teams.Team.Commands.CreateTeam
{
    public sealed class CreateTeamValidator : AbstractValidator<CreateTeamCommand>
    {
        public CreateTeamValidator()
        {
            RuleFor(C => C.Name).Must(name => !string.IsNullOrWhiteSpace(name)).MaximumLength(100);

            RuleFor(C => C.Description).MaximumLength(500);
        }
    }
}