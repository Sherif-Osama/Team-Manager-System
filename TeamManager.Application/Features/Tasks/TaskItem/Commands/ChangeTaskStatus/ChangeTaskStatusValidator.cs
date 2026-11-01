using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.ChangeTaskStatus
{
    public sealed class ChangeTaskStatusValidator : AbstractValidator<ChangeTaskStatusCommand>
    {
        public ChangeTaskStatusValidator()
        {
            RuleFor(x => x.TaskId)
                .GreaterThan(0)
                .WithMessage("Task ID must be greater than 0.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid task status.");
        }
    }
}
