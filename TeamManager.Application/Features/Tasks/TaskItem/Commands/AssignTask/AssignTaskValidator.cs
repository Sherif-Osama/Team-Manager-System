using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.AssignTask
{
    public sealed class AssignTaskValidator : AbstractValidator<AssignTaskCommand>
    {
        public AssignTaskValidator()
        {
            RuleFor(x => x.TaskId)
                .GreaterThan(0)
                .WithMessage("Task ID must be greater than 0.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required.");
        }
    }
}
