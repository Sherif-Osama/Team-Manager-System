using FluentValidation;

namespace TeamManager.Application.Features.Projects.Project.Queries.GetProjectByName
{
    public sealed class GetProjectByNameValidator : AbstractValidator<GetProjectByNameQuery>
    {
        public GetProjectByNameValidator()
        {
            RuleFor(x => x.Name).Must(name => !string.IsNullOrWhiteSpace(name)).MaximumLength(100);
        }
    }
}