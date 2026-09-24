using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;
using TeamManager.Application.Common.Exceptions.UserExceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.Project.Commands.TransferProjectOwnership
{
    public sealed class TransferProjectOwnershipCommandHandler(ICurrentUser currentUser, IProjectRepository projectRepository,
        IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<TransferProjectOwnershipCommand>
    {
        public async Task Handle(TransferProjectOwnershipCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue || !currentUser.IsAuthenticated)
                throw new UnauthorizedAccessException("User is not authenticated.");

            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var newOwner = await userRepository.GetByIdAsync(request.NewOwnerUserId, ct);

                if (newOwner is null || !newOwner.IsActive)
                    throw new UserNotFoundException(request.NewOwnerUserId);

                var project = await projectRepository.GetByIdWithMembersAsync(request.ProjectId, ct);

                if (project is null)
                    throw new ProjectNotFoundException(request.ProjectId);

                var isCurrentOwner = await projectRepository.HasActiveRoleAsync(request.ProjectId,
                    currentUser.UserId.Value, [ProjectRole.Owner], ct);

                if (!isCurrentOwner)
                    throw new ForbiddenException("Only the current project owner can transfer ownership.");

                project.TransferOwnership(request.NewOwnerUserId);

            }, cancellationToken);
        }
    }
}