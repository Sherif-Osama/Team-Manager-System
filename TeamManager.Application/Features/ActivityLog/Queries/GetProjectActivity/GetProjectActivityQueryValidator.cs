using FluentValidation;

namespace TeamManager.Application.Features.ActivityLog.Queries.GetProjectActivity
{
    public sealed class GetProjectActivityQueryValidator : AbstractValidator<GetProjectActivityQuery>
    {
        public GetProjectActivityQueryValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("Project ID is required.");

            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page must be greater than 0.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100.");
        }
    }
}
