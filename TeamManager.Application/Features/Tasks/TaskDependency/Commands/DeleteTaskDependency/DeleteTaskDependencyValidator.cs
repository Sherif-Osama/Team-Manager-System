using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskDependency.Commands.DeleteTaskDependency
{
    public sealed class DeleteTaskDependencyValidator : AbstractValidator<DeleteTaskDependencyCommand>
    {
        public DeleteTaskDependencyValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.DependencyId).GreaterThan(0);
        }
    }
}