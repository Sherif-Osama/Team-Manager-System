using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskComment.Commands.AddTaskComment
{
    public sealed class AddTaskCommentCommandValidator : AbstractValidator<AddTaskCommentCommand>
    {
        public AddTaskCommentCommandValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        }
    }
}