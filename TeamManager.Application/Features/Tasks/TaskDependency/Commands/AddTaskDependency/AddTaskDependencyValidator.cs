using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskDependency.Commands.AddTaskDependency
{
    public sealed class AddTaskDependencyValidator : AbstractValidator<AddTaskDependencyCommand>
    {
        public AddTaskDependencyValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.DependsOnTaskId).GreaterThan(0);
        }
    }
}