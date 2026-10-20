using FluentValidation;

namespace TeamManager.Application.Features.Projects.Project.Commands.DeleteProject
{
    public sealed class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
    {
        public DeleteProjectCommandValidator()
        {
            RuleFor(x => x.ProjectId).NotEmpty();
        }
    }
}
