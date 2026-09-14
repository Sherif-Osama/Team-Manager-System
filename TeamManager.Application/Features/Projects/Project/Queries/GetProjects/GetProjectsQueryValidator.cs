using FluentValidation;

namespace TeamManager.Application.Features.Projects.Project.Queries.GetProjects
{
    public sealed class GetProjectsQueryValidator : AbstractValidator<GetProjectsQuery>
    {
        public GetProjectsQueryValidator()
        {
            RuleFor(x => x.Search).MaximumLength(150);

            RuleFor(x => x.Status).IsInEnum();

            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}