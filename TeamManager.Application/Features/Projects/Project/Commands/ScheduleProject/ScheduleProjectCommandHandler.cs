using MediatR;
using TeamManager.Application.Abstractions;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Projects.Project.Commands.ScheduleProject
{
    public sealed class ScheduleProjectCommandHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<ScheduleProjectCommand>
    {
        public async Task Handle(ScheduleProjectCommand request, CancellationToken cancellationToken)
        {
            var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

            if (project is null)
                throw new ProjectNotFoundException(request.ProjectId);

            project.Schedule(request.StartDate, request.DueDate);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}