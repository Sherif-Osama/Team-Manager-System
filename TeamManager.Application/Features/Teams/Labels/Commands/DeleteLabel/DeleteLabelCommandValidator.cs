using FluentValidation;

namespace TeamManager.Application.Features.Teams.Labels.Commands.DeleteLabel
{
    public sealed class DeleteLabelCommandValidator : AbstractValidator<DeleteLabelCommand>
    {
        public DeleteLabelCommandValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();

            RuleFor(x => x.LabelId).GreaterThan(0);
        }
    }
}