using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskChecklist.Commands.AddChecklistItem
{
    public sealed class AddChecklistItemValidator : AbstractValidator<AddChecklistItemCommand>
    {
        public AddChecklistItemValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.Content).NotEmpty().MaximumLength(300);
        }
    }
}