using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.DeleteTask
{
    public sealed class DeleteTaskValidator : AbstractValidator<DeleteTaskCommand>
    {
        public DeleteTaskValidator()
        {
            RuleFor(x => x.TaskId)
                .GreaterThan(0)
                .WithMessage("Task ID must be greater than 0.");
        }
    }
}
