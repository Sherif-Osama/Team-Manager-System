using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.RescheduleTask
{
    public sealed class RescheduleTaskValidator : AbstractValidator<RescheduleTaskCommand>
    {
        public RescheduleTaskValidator()
        {

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.StartDate).GreaterThanOrEqualTo(today).When(x => x.StartDate.HasValue)
                .WithMessage("Project start date cannot be in the past.");

            RuleFor(x => x.DueDate).GreaterThanOrEqualTo(today).When(x => x.DueDate.HasValue)
                .WithMessage("Project due date cannot be in the past.");

            RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.StartDate.HasValue && x.DueDate.HasValue)
                .WithMessage("A project's due date cannot be before its start date.");
        }
    }
}