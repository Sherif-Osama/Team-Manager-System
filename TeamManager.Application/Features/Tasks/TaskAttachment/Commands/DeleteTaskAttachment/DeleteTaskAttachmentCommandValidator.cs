using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskAttachment.Commands.DeleteTaskAttachment
{
    public sealed class DeleteTaskAttachmentCommandValidator : AbstractValidator<DeleteTaskAttachmentCommand>
    {
        public DeleteTaskAttachmentCommandValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.AttachmentId).GreaterThan(0);
        }
    }
}