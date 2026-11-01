using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskItem.Queries.GetTask
{
    public sealed class GetTaskQueryValidator : AbstractValidator<GetTaskQuery>
    {
        public GetTaskQueryValidator()
        {
            RuleFor(x => x.TaskId)
                .GreaterThan(0)
                .WithMessage("Task ID must be greater than 0.");
        }
    }
}
