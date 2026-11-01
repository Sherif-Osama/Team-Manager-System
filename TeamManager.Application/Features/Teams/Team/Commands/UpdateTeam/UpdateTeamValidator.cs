using FluentValidation;

namespace TeamManager.Application.Features.Teams.Team.Commands.UpdateTeam
{
    public sealed class UpdateTeamValidator : AbstractValidator<UpdateTeamCommand>
    {
        public UpdateTeamValidator()
        {
            RuleFor(C => C.TeamId)
                .NotEmpty()
                .WithMessage("Team ID is required.");

            RuleFor(C => C.Name)
                .NotEmpty()
                .WithMessage("Team name is required.")
                .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Team name cannot be whitespace.")
                .MaximumLength(100)
                .WithMessage("Team name must not exceed 100 characters.");

            RuleFor(C => C.Description)
                .MaximumLength(500)
                .WithMessage("Team description must not exceed 500 characters.");
        }
    }
}
