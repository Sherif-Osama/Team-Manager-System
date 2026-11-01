using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskChecklist.Commands.RemoveChecklistItem
{
    public sealed class RemoveChecklistItemCommandValidator : AbstractValidator<RemoveChecklistItemCommand>
    {
        public RemoveChecklistItemCommandValidator()
        {
            RuleFor(x => x.TaskId)
                .GreaterThan(0)
                .WithMessage("Task ID must be greater than 0.");

            RuleFor(x => x.ChecklistItemId)
                .GreaterThan(0)
                .WithMessage("Checklist item ID must be greater than 0.");
        }
    }
}
