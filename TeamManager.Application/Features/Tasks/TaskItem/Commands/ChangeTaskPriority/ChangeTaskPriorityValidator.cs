using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.ChangeTaskPriority
{
    public sealed class ChangeTaskPriorityValidator : AbstractValidator<ChangeTaskPriorityCommand>
    {
        public ChangeTaskPriorityValidator()
        {
            RuleFor(x => x.TaskId)
                .GreaterThan(0)
                .WithMessage("Task ID must be greater than 0.");

            RuleFor(x => x.Priority)
                .IsInEnum()
                .WithMessage("Invalid task priority.");
        }
    }
}
