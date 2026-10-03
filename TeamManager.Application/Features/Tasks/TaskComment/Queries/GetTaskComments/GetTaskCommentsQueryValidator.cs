using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskComment.Queries.GetTaskComments
{
    public sealed class GetTaskCommentsQueryValidator : AbstractValidator<GetTaskCommentsQuery>
    {
        public GetTaskCommentsQueryValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}