using FluentValidation;

namespace TeamManager.Application.Features.Teams.Labels.Commands.CreateLabel
{
    public sealed class CreateLabelCommandValidator
        : AbstractValidator<CreateLabelCommand>
    {
        public CreateLabelCommandValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty()
                .WithMessage("Team ID is required.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Label name is required.")
                .MaximumLength(50)
                .WithMessage("Label name must not exceed 50 characters.");

            RuleFor(x => x.ColorHex)
                .Matches("^#[0-9A-Fa-f]{6}$")
                .WithMessage("Color hex must be a valid 6-character hex code (e.g. #FFFFFF).")
                .When(x => !string.IsNullOrWhiteSpace(x.ColorHex));
        }
    }
}
