using FluentValidation;

namespace TeamManager.Application.Features.ActivityLog.Queries.GetProjectActivity
{
    public sealed class GetProjectActivityQueryValidator : AbstractValidator<GetProjectActivityQuery>
    {
        public GetProjectActivityQueryValidator()
        {
            RuleFor(x => x.ProjectId).NotEmpty();

            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}