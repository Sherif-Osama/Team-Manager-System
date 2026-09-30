using FluentValidation;

namespace TeamManager.Application.Features.Teams.Labels.Commands.UpdateLabel
{
    public sealed class UpdateLabelCommandValidator : AbstractValidator<UpdateLabelCommand>
    {
        public UpdateLabelCommandValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();

            RuleFor(x => x.LabelId).GreaterThan(0);

            RuleFor(x => x.Name).NotEmpty().MaximumLength(50);

            RuleFor(x => x.ColorHex).Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrWhiteSpace(x.ColorHex));
        }
    }
}