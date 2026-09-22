using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.AssignTask
{
    public sealed class AssignTaskValidator : AbstractValidator<AssignTaskCommand>
    {
        public AssignTaskValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}