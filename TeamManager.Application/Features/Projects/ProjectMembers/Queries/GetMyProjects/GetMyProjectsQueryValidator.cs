using FluentValidation;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Queries.GetMyProjects
{
    public sealed class GetMyProjectsQueryValidator : AbstractValidator<GetMyProjectsQuery>
    {
        public GetMyProjectsQueryValidator()
        {
            RuleFor(x => x.Search)
                .MaximumLength(150)
                .WithMessage("Search term must not exceed 150 characters.");

            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage("Invalid project role.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid project status.");

            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page must be greater than 0.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100.");
        }
    }
}
