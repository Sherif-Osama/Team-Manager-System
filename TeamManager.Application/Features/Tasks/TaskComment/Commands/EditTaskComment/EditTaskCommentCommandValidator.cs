using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskComment.Commands.EditTaskComment
{
    public sealed class EditTaskCommentCommandValidator : AbstractValidator<EditTaskCommentCommand>
    {
        public EditTaskCommentCommandValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.CommentId).GreaterThan(0);

            RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);

            RuleFor(x => x.MentionedUserIds).Must(ids => ids == null || ids.All(id => id != Guid.Empty))
                .WithMessage("Mentioned user IDs cannot be empty.");
        }
    }
}