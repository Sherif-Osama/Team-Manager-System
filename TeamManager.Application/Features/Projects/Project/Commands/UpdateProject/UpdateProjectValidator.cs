using FluentValidation;

namespace TeamManager.Application.Features.Projects.Project.Commands.UpdateProject
{
    public sealed class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
    {
        public UpdateProjectValidator()
        {
            RuleFor(c => c.ProjectId).NotEmpty();

            RuleFor(c => c.Name).NotEmpty().MaximumLength(150);

            RuleFor(c => c.Description).MaximumLength(1000);
        }
    }
}