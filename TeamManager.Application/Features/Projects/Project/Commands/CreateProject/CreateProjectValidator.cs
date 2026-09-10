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
        }
    }
}