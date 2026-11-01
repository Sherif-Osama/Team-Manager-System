using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskChecklist.Commands.AddChecklistItem
{
    public sealed class AddChecklistItemValidator : AbstractValidator<AddChecklistItemCommand>
    {
        public AddChecklistItemValidator()
        {
            RuleFor(x => x.TaskId)
                .GreaterThan(0)
                .WithMessage("Task ID must be greater than 0.");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Checklist item content is required.")
                .MaximumLength(300)
                .WithMessage("Checklist item content must not exceed 300 characters.");
        }
    }
}
