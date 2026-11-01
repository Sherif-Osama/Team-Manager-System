using FluentValidation;

namespace TeamManager.Application.Features.Teams.Labels.Queries.GetLabel
{
    public sealed class GetLabelByIdQueryValidator : AbstractValidator<GetLabelByIdQuery>
    {
        public GetLabelByIdQueryValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty()
                .WithMessage("Team ID is required.");

            RuleFor(x => x.LabelId)
                .GreaterThan(0)
                .WithMessage("Label ID must be greater than 0.");
        }
    }
}
