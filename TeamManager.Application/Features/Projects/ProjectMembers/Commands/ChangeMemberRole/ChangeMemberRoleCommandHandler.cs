using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.ChangeMemberRole
{
    public sealed class ChangeMemberRoleCommandHandler(IProjectRepository projectRepository, ICurrentUser currentUser,
        IUnitOfWork unitOfWork) : IRequestHandler<ChangeMemberRoleCommand>
    {
        public async Task Handle(ChangeMemberRoleCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var project = await projectRepository.GetByIdWithMembersAsync(request.ProjectId, cancellationToken);

            if (project is null)
                throw new ProjectNotFoundException(request.ProjectId);

            project.ChangeMemberRole(request.ProjectMemberId, request.Role, currentUser.UserId.Value);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}