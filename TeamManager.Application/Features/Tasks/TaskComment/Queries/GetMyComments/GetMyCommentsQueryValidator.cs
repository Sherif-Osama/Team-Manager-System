using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskComment.Queries.GetMyComments
{
    public sealed class GetMyCommentsQueryValidator : AbstractValidator<GetMyCommentsQuery>
    {
        public GetMyCommentsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}