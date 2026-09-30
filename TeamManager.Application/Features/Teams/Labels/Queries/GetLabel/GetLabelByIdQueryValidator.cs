using FluentValidation;

namespace TeamManager.Application.Features.Teams.Labels.Queries.GetLabel
{
    public sealed class GetLabelByIdQueryValidator : AbstractValidator<GetLabelByIdQuery>
    {
        public GetLabelByIdQueryValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();

            RuleFor(x => x.LabelId).GreaterThan(0);
        }
    }
}