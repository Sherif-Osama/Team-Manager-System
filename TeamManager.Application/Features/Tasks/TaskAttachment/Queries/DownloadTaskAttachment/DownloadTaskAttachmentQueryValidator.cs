using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskAttachment.Queries.DownloadTaskAttachment
{
    public sealed class DownloadTaskAttachmentQueryValidator : AbstractValidator<DownloadTaskAttachmentQuery>
    {
        public DownloadTaskAttachmentQueryValidator()
        {
            RuleFor(x => x.TaskId)
                .GreaterThan(0)
                .WithMessage("Task ID must be greater than 0.");

            RuleFor(x => x.AttachmentId)
                .GreaterThan(0)
                .WithMessage("Attachment ID must be greater than 0.");
        }
    }
}
