using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskAttachment.Queries.GetTaskAttachments
{
    public sealed class GetTaskAttachmentsQueryValidator : AbstractValidator<GetTaskAttachmentsQuery>
    {
        public GetTaskAttachmentsQueryValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}