using MediatR;
using TeamManager.Application.Abstractions;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.AddProjectMember
{
    public sealed class AddProjectMemberCommandHandler(ICurrentUser currentUser, IProjectRepository projectRepository,
        IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<AddProjectMemberCommand>
    {
        public async Task Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var project = await projectRepository.GetByIdWithMembersAsync(request.ProjectId, cancellationToken);

            if (project is null)
                throw new ProjectNotFoundException(request.ProjectId);

            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

            if (user is null || !user.IsActive)
                throw new UserNotFoundException(request.UserId);

            project.AddMember(request.UserId, request.ProjectRole, currentUser.UserId!.Value);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}