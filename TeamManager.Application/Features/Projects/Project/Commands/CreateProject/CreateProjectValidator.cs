using FluentValidation;

namespace TeamManager.Application.Features.Projects.Project.Commands.CreateProject
{
    public sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
    {
        public CreateProjectValidator()
        {
            RuleFor(c => c.TeamId).NotEmpty();

            RuleFor(c => c.Name).NotEmpty().MaximumLength(150);

            RuleFor(c => c.Description).MaximumLength(1000);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            RuleFor(c => c.StartDate).GreaterThanOrEqualTo(today).When(c => c.StartDate.HasValue)
                .WithMessage("Project start date cannot be in the past.");

            RuleFor(c => c.DueDate).GreaterThanOrEqualTo(today).When(c => c.DueDate.HasValue)
                .WithMessage("Project due date cannot be in the past.");

            RuleFor(c => c.DueDate).GreaterThanOrEqualTo(c => c.StartDate)
                .When(c => c.StartDate.HasValue && c.DueDate.HasValue).WithMessage("A project's due date cannot be before its start date.");
        }
    }
}