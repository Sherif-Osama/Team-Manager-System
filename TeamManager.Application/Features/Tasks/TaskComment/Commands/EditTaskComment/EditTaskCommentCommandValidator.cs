using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskComment.Commands.EditTaskComment
{
    public sealed class EditTaskCommentCommandValidator : AbstractValidator<EditTaskCommentCommand>
    {
        public EditTaskCommentCommandValidator()
        {
            RuleFor(x => x.TaskId)
                .GreaterThan(0)
                .WithMessage("Task ID must be greater than 0.");

            RuleFor(x => x.CommentId)
                .GreaterThan(0)
                .WithMessage("Comment ID must be greater than 0.");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Comment content is required.")
                .MaximumLength(2000)
                .WithMessage("Comment content must not exceed 2000 characters.");

            RuleFor(x => x.MentionedUserIds)
                .Must(ids => ids == null || ids.All(id => id != Guid.Empty))
                .WithMessage("Mentioned user IDs cannot be empty.");
        }
    }
}
