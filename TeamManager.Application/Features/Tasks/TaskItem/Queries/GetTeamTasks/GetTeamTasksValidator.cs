using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskItem.Queries.GetTeamTasks
{
    public sealed class GetTeamTasksValidator : AbstractValidator<GetTeamTasksQuery>
    {
        public GetTeamTasksValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();

            RuleFor(x => x.Search).MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.Search));

            RuleFor(x => x.ProjectId).NotEmpty().When(x => x.ProjectId.HasValue);

            RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);

            RuleFor(x => x.Priority).IsInEnum().When(x => x.Priority.HasValue);

            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
