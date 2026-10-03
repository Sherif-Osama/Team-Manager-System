using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskComment.Queries.GetMyMentions
{
    public sealed class GetMyMentionsQueryValidator : AbstractValidator<GetMyMentionsQuery>
    {
        public GetMyMentionsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}