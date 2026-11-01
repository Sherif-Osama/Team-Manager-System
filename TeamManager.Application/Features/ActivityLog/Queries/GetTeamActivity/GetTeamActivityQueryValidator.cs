using FluentValidation;

namespace TeamManager.Application.Features.ActivityLog.Queries.GetTeamActivity
{
    public sealed class GetTeamActivityQueryValidator : AbstractValidator<GetTeamActivityQuery>
    {
        public GetTeamActivityQueryValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty()
                .WithMessage("Team ID is required.");

            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page must be greater than 0.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100.");
        }
    }
}
