using FluentValidation;

namespace TeamManager.Application.Features.Projects.Project.Queries.GetProjectByName
{
    public sealed class GetProjectByNameValidator : AbstractValidator<GetProjectByNameQuery>
    {
        public GetProjectByNameValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Project name is required.")
                .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Project name cannot be whitespace.")
                .MaximumLength(100)
                .WithMessage("Project name must not exceed 100 characters.");
        }
    }
}
