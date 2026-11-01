using FluentValidation;

namespace TeamManager.Application.Features.Projects.Project.Commands.UpdateProject
{
    public sealed class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
    {
        public UpdateProjectValidator()
        {
            RuleFor(c => c.ProjectId)
                .NotEmpty()
                .WithMessage("Project ID is required.");

            RuleFor(c => c.Name)
                .NotEmpty()
                .WithMessage("Project name is required.")
                .MaximumLength(150)
                .WithMessage("Project name must not exceed 150 characters.");

            RuleFor(c => c.Description)
                .MaximumLength(1000)
                .WithMessage("Project description must not exceed 1000 characters.");
        }
    }
}
