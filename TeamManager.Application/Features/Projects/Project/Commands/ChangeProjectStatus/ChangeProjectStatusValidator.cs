using FluentValidation;

namespace TeamManager.Application.Features.Projects.Project.Commands.ChangeProjectStatus
{
    public sealed class ChangeProjectStatusValidator : AbstractValidator<ChangeProjectStatusCommand>
    {
        public ChangeProjectStatusValidator()
        {
            RuleFor(c => c.ProjectId)
                .NotEmpty()
                .WithMessage("Project ID is required.");

            RuleFor(c => c.Status)
                .IsInEnum()
                .WithMessage("Invalid project status.");
        }
    }
}
