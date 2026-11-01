using FluentValidation;

namespace TeamManager.Application.Features.Teams.Labels.Commands.DeleteLabel
{
    public sealed class DeleteLabelCommandValidator : AbstractValidator<DeleteLabelCommand>
    {
        public DeleteLabelCommandValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty()
                .WithMessage("Team ID is required.");

            RuleFor(x => x.LabelId)
                .GreaterThan(0)
                .WithMessage("Label ID must be greater than 0.");
        }
    }
}
