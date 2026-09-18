using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.CreateTask
{
    public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
    {
        public CreateTaskCommandValidator()
        {
            RuleFor(x => x.ProjectId).NotEmpty();

            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);

            RuleFor(x => x.Description).MaximumLength(2000);

            RuleFor(x => x.AssigneeUserId).Must(id => id == null || id != Guid.Empty);

            RuleFor(x => x.Priority).IsInEnum();
        }
    }
}