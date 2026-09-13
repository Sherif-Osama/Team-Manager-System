using MediatR;
using TeamManager.Application.Abstractions;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.RemoveMember
{
    public sealed class RemoveProjectMemberCommandHandler(IProjectRepository projectRepository, ICurrentUser currentUser, IUnitOfWork unitOfWork)
        : IRequestHandler<RemoveMemberCommand>
    {
        public async Task Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var project = await projectRepository.GetByIdWithMembersAsync(request.ProjectId, cancellationToken);

            if (project is null)
                throw new ProjectNotFoundException(request.ProjectId);

            project.RemoveMember(request.MemberId, currentUser.UserId.Value);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}