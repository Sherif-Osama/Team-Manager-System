using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskLabel.Commands.AddTaskLabel
{
    public sealed class AddTaskLabelCommandValidator : AbstractValidator<AddTaskLabelCommand>
    {
        public AddTaskLabelCommandValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.LabelId).GreaterThan(0);
        }
    }
}