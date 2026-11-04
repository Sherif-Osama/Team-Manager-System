using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;

namespace TeamManager.Application.Features.Projects.Project.Commands.DeleteProject
{
    public sealed class DeleteProjectCommandHandler(IProjectRepository projectRepository, ITaskRepository taskRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<DeleteProjectCommand>
    {
        public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
        {
            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var project = await projectRepository.GetByIdAsync(request.ProjectId, ct);

                if (project is null)
                    throw new ProjectNotFoundException(request.ProjectId);

                var hasActiveTasks = await taskRepository.HasActiveTasksAsync(request.ProjectId, ct);

                if (hasActiveTasks)
                    throw new ProjectHasActiveTasksException(request.ProjectId);

                project.SoftDelete();
            }, cancellationToken);
        }
    }
}
