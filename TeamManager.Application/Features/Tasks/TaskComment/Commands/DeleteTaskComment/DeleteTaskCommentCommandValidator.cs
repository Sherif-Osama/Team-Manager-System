using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskComment.Commands.DeleteTaskComment
{
    public sealed class DeleteTaskCommentCommandValidator : AbstractValidator<DeleteTaskCommentCommand>
    {
        public DeleteTaskCommentCommandValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.CommentId).GreaterThan(0);
        }
    }
}