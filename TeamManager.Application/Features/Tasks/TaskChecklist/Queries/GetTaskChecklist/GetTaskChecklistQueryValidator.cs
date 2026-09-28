using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskChecklist.Queries.GetTaskChecklist
{
    public sealed class GetTaskChecklistQueryValidator : AbstractValidator<GetTaskChecklistQuery>
    {
        public GetTaskChecklistQueryValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);
        }
    }
}