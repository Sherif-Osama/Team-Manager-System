using FluentValidation;

namespace TeamManager.Application.Features.Projects.Project.Queries.GetProject
{
    public sealed class GetProjectQueryValidator : AbstractValidator<GetProjectQuery>
    {
        public GetProjectQueryValidator()
        {
            RuleFor(x => x.ProjectId).NotEmpty();
        }
    }
}