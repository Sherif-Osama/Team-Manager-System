using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskLabel.Command.RemoveTaskLabel
{
    public sealed class RemoveTaskLabelCommandValidator : AbstractValidator<RemoveTaskLabelCommand>
    {
        public RemoveTaskLabelCommandValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.LabelId).GreaterThan(0);
        }
    }
}