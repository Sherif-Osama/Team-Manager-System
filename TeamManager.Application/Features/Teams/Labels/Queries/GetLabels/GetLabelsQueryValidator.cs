using FluentValidation;

namespace TeamManager.Application.Features.Teams.Labels.Queries.GetLabels
{
    public sealed class GetLabelsQueryValidator : AbstractValidator<GetLabelsQuery>
    {
        public GetLabelsQueryValidator()
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
