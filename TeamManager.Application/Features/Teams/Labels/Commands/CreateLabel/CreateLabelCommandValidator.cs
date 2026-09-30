using FluentValidation;

namespace TeamManager.Application.Features.Teams.Labels.Commands.CreateLabel
{
    public sealed class CreateLabelCommandValidator
        : AbstractValidator<CreateLabelCommand>
    {
        public CreateLabelCommandValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();

            RuleFor(x => x.Name).NotEmpty().MaximumLength(50);

            RuleFor(x => x.ColorHex).Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrWhiteSpace(x.ColorHex));
        }
    }
}