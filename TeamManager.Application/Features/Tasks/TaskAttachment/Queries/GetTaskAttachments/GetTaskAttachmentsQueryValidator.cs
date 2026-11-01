using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskAttachment.Queries.GetTaskAttachments
{
    public sealed class GetTaskAttachmentsQueryValidator : AbstractValidator<GetTaskAttachmentsQuery>
    {
        public GetTaskAttachmentsQueryValidator()
        {
            RuleFor(x => x.TaskId)
                .GreaterThan(0)
                .WithMessage("Task ID must be greater than 0.");

            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page must be greater than 0.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100.");

            RuleFor(x => x.Search)
                .MaximumLength(255)
                .WithMessage("Search term must not exceed 255 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Search));
        }
    }
}
