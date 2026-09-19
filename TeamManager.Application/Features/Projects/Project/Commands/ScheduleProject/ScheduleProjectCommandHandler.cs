using MediatR;
using TeamManager.Application.Abstractions;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Projects.Project.Commands.ScheduleProject
{
    public sealed class ScheduleProjectCommandHandler(IProjectRepository projectRepository, ITaskRepository taskRepository
        , IUnitOfWork unitOfWork) : IRequestHandler<ScheduleProjectCommand>
    {
        public async Task Handle(ScheduleProjectCommand request, CancellationToken cancellationToken)
        {
            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var project = await projectRepository.GetByIdAsync(request.ProjectId, ct);

                if (project is null) throw new ProjectNotFoundException(request.ProjectId);

                project.Schedule(request.StartDate, request.DueDate);

                var conflictingTitles = await taskRepository.GetConflictingWithProjectDatesAsync(project.Id, project.StartDate,
                    project.DueDate, maxResults: 3, ct);

                if (conflictingTitles.Count > 0)
                    throw new TaskDateConflictException(conflictingTitles);

                await unitOfWork.SaveChangesAsync(ct);

            }, cancellationToken);
        }
    }
}