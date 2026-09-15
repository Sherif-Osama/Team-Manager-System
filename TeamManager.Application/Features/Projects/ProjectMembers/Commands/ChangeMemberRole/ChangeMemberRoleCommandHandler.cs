using MediatR;
using TeamManager.Application.Abstractions;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.ChangeMemberRole
{
    public sealed class ChangeMemberRoleCommandHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<ChangeMemberRoleCommand>
    {
        public async Task Handle(ChangeMemberRoleCommand request, CancellationToken cancellationToken)
        {
            var project = await projectRepository.GetByIdWithMembersAsync(request.ProjectId, cancellationToken);

            if (project is null)
                throw new ProjectNotFoundException(request.ProjectId);

            project.ChangeMemberRole(request.ProjectMemberId, request.Role);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}