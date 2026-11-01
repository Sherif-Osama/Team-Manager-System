using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.CreateTask
{
    public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
    {
        public CreateTaskCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("Project ID is required.");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Task title is required.")
                .MaximumLength(200)
                .WithMessage("Task title must not exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .WithMessage("Task description must not exceed 2000 characters.");

            RuleFor(x => x.AssigneeUserId)
                .Must(id => id == null || id != Guid.Empty)
                .WithMessage("Assignee user ID cannot be empty.");

            RuleFor(x => x.Priority)
                .IsInEnum()
                .WithMessage("Invalid task priority.");
        }
    }
}
