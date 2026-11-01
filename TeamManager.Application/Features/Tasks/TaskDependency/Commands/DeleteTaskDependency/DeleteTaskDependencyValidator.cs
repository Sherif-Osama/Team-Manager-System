using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskDependency.Commands.DeleteTaskDependency
{
    public sealed class DeleteTaskDependencyValidator : AbstractValidator<DeleteTaskDependencyCommand>
    {
        public DeleteTaskDependencyValidator()
        {
            RuleFor(x => x.TaskId)
                .GreaterThan(0)
                .WithMessage("Task ID must be greater than 0.");

            RuleFor(x => x.DependencyId)
                .GreaterThan(0)
                .WithMessage("Dependency ID must be greater than 0.");
        }
    }
}
