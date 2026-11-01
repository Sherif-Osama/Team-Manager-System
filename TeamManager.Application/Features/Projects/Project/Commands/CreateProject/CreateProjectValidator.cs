using FluentValidation;

namespace TeamManager.Application.Features.Projects.Project.Commands.CreateProject
{
    public sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
    {
        public CreateProjectValidator()
        {
            RuleFor(c => c.TeamId)
                .NotEmpty()
                .WithMessage("Team ID is required.");

            RuleFor(c => c.Name)
                .NotEmpty()
                .WithMessage("Project name is required.")
                .MaximumLength(150)
                .WithMessage("Project name must not exceed 150 characters.");

            RuleFor(c => c.Description)
                .MaximumLength(1000)
                .WithMessage("Project description must not exceed 1000 characters.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            RuleFor(c => c.StartDate)
                .GreaterThanOrEqualTo(today)
                .When(c => c.StartDate.HasValue)
                .WithMessage("Project start date cannot be in the past.");

            RuleFor(c => c.DueDate)
                .GreaterThanOrEqualTo(today)
                .When(c => c.DueDate.HasValue)
                .WithMessage("Project due date cannot be in the past.");

            RuleFor(c => c.DueDate)
                .GreaterThanOrEqualTo(c => c.StartDate)
                .When(c => c.StartDate.HasValue && c.DueDate.HasValue)
                .WithMessage("A project's due date cannot be before its start date.");
        }
    }
}
