using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskDependency.Commands.AddTaskDependency
{
    public sealed class AddTaskDependencyValidator : AbstractValidator<AddTaskDependencyCommand>
    {
        public AddTaskDependencyValidator()
        {
            RuleFor(x => x.TaskId)
                .GreaterThan(0)
                .WithMessage("Task ID must be greater than 0.");

            RuleFor(x => x.DependsOnTaskId)
                .GreaterThan(0)
                .WithMessage("Depends on task ID must be greater than 0.");
        }
    }
}
