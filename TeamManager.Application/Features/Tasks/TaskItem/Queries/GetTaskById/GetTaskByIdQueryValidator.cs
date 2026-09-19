using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskItem.Queries.GetTaskById
{
    public sealed class GetTaskByIdQueryValidator : AbstractValidator<GetTaskByIdQuery>
    {
        public GetTaskByIdQueryValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);
        }
    }
}