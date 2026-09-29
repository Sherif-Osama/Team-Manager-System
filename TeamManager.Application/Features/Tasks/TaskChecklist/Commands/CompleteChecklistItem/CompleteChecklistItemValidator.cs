using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskChecklist.Commands.CompleteChecklistItem
{
    public sealed class CompleteChecklistItemValidator : AbstractValidator<CompleteChecklistItemCommand>
    {
        public CompleteChecklistItemValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.ChecklistItemId).GreaterThan(0);
        }
    }
}