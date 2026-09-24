using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.RemoveMember
{
    public sealed class RemoveProjectMemberCommandHandler(IProjectRepository projectRepository, ICurrentUser currentUser,
        ITaskRepository taskRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<RemoveMemberCommand>
    {
        public async Task Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var project = await projectRepository.GetByIdWithMembersAsync(request.ProjectId, cancellationToken);

            if (project is null)
                throw new ProjectNotFoundException(request.ProjectId);

            var memberToRemove = project.Members.FirstOrDefault(m => m.Id == request.MemberId);

            if (memberToRemove is null)
                throw new ProjectMemberNotFoundException(project.Id, request.MemberId);

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                project.RemoveMember(request.MemberId, currentUser.UserId.Value);
                await taskRepository.UnassignActiveTasksByProjectAsync(project.Id, memberToRemove.UserId, ct);
            }, cancellationToken);
        }
    }
}