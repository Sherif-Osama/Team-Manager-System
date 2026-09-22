using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.UnassignTask
{
    public sealed class UnassignTaskValidator : AbstractValidator<UnassignTaskCommand>
    {
        public UnassignTaskValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);
        }
    }
}