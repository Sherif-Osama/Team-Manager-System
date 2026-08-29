using FluentValidation;

namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeams
{
    public sealed class GetTeamsValidator : AbstractValidator<GetTeamsQuery>
    {
        public GetTeamsValidator()
        {
            RuleFor(x => x.Search).MaximumLength(100);

            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}