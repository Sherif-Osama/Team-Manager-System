using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskComment.Commands.AddTaskComment
{
    public sealed class AddTaskCommentCommandValidator : AbstractValidator<AddTaskCommentCommand>
    {
        public AddTaskCommentCommandValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);

            RuleFor(x => x.MentionedUserIds).Must(ids => ids == null || ids.All(id => id != Guid.Empty))
                .WithMessage("Mentioned user IDs cannot be empty.");
        }
    }
}