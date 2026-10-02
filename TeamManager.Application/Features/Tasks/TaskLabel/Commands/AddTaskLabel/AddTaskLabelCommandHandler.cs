using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;
using TeamManager.Application.Common.Exceptions.TaskExceptions;
using TeamManager.Application.Common.Exceptions.TeamExceptions;

namespace TeamManager.Application.Features.Tasks.TaskLabel.Commands.AddTaskLabel
{
    public sealed class AddTaskLabelCommandHandler(ITaskRepository taskRepository, IProjectRepository projectRepository,
        ITeamRepository teamRepository, IUnitOfWork unitOfWork) : IRequestHandler<AddTaskLabelCommand>
    {
        public async Task Handle(AddTaskLabelCommand request, CancellationToken cancellationToken)
        {
            var task = await taskRepository.GetByIdWithLabelsAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            var project = await projectRepository.GetByIdAsync(task.ProjectId, cancellationToken);

            if (project is null)
                throw new ProjectNotFoundException(task.ProjectId);

            var labelBelongsToTeam = await teamRepository.LabelExistsInTeamAsync(project.TeamId, request.LabelId, cancellationToken);

            if (!labelBelongsToTeam)
                throw new LabelNotFoundException(request.LabelId);

            task.AddLabel(request.LabelId);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
