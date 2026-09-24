using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskDependency.Queries.GetTaskDependencies
{
    public sealed class GetTaskDependenciesValidator : AbstractValidator<GetTaskDependenciesQuery>
    {
        public GetTaskDependenciesValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}