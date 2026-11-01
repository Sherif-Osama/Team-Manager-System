using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskItem.Queries.GetMyTasks
{
    public sealed class GetMyTasksValidator : AbstractValidator<GetMyTasksQuery>
    {
        public GetMyTasksValidator()
        {
            RuleFor(x => x.Search)
                .MaximumLength(150)
                .WithMessage("Search term must not exceed 150 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Search));

            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("Project ID must not be empty.")
                .When(x => x.ProjectId.HasValue);

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid task status.")
                .When(x => x.Status.HasValue);

            RuleFor(x => x.Priority)
                .IsInEnum()
                .WithMessage("Invalid task priority.")
                .When(x => x.Priority.HasValue);

            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page must be greater than 0.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100.");
        }
    }
}
