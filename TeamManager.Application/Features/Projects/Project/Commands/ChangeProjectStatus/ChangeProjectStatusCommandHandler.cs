using MediatR;
using TeamManager.Application.Abstractions;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Projects.Project.Commands.ChangeProjectStatus
{
    public sealed class ChangeProjectStatusCommandHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<ChangeProjectStatusCommand>
    {
        public async Task Handle(ChangeProjectStatusCommand request, CancellationToken cancellationToken)
        {
            var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

            if (project is null)
                throw new ProjectNotFoundException(request.ProjectId);

            project.ChangeStatus(request.Status);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}