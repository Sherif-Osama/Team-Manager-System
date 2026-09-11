using MediatR;
using TeamManager.Application.Abstractions;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Projects.Project.Commands.CreateProject
{
    public sealed class CreateProjectCommandHandler(ICurrentUser currentUser, IUnitOfWork unitOfWork,
        IProjectRepository projectRepository) : IRequestHandler<CreateProjectCommand, Guid>
    {
        public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue || !currentUser.IsAuthenticated)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            var existingProject = await projectRepository.GetByNameAsync(request.TeamId, request.Name, cancellationToken);

            if (existingProject is not null)
                throw new ProjectNameAlreadyExistsException(request.Name);

            var project = new Domain.Entities.Project(Guid.NewGuid(), request.TeamId, request.Name,
                currentUser.UserId.Value, request.Description, request.StartDate, request.DueDate);

            await projectRepository.AddAsync(project, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return project.Id;
        }
    }
}