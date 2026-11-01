using FluentValidation;

namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeams
{
    public sealed class GetTeamsValidator : AbstractValidator<GetTeamsQuery>
    {
        public GetTeamsValidator()
        {
            RuleFor(x => x.Search)
                .MaximumLength(100)
                .WithMessage("Search term must not exceed 100 characters.");

            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page must be greater than 0.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100.");
        }
    }
}
