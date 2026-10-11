using FluentValidation;

namespace TeamManager.Application.Features.ActivityLog.Queries.GetTeamActivity
{
    public sealed class GetTeamActivityQueryValidator : AbstractValidator<GetTeamActivityQuery>
    {
        public GetTeamActivityQueryValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();

            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}