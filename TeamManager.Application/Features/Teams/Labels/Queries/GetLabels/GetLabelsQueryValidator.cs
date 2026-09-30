using FluentValidation;

namespace TeamManager.Application.Features.Teams.Labels.Queries.GetLabels
{
    public sealed class GetLabelsQueryValidator : AbstractValidator<GetLabelsQuery>
    {
        public GetLabelsQueryValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();

            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}