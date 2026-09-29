using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskChecklist.Commands.ReopenChecklistItem
{
    public sealed class ReopenChecklistItemCommandValidator : AbstractValidator<ReopenChecklistItemCommand>
    {
        public ReopenChecklistItemCommandValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.ChecklistItemId).GreaterThan(0);
        }
    }
}