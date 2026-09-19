using FluentValidation;
namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.UpdateTask
{
    public sealed class UpdateTaskValidator : AbstractValidator<UpdateTaskCommand>
    {
        public UpdateTaskValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);

            RuleFor(x => x.Description).MaximumLength(2000);
        }
    }
}