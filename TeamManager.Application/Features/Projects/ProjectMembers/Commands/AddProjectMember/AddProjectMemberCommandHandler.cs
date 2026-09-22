using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.AddProjectMember
{
    public sealed class AddProjectMemberCommandHandler(ICurrentUser currentUser, IProjectRepository projectRepository,
        IUserRepository userRepository, ITeamRepository teamRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<AddProjectMemberCommand, long>
    {
        public async Task<long> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            long memberId = default;

            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var project = await projectRepository.GetByIdWithMembersAsync(request.ProjectId, cancellationToken);

                if (project is null)
                    throw new ProjectNotFoundException(request.ProjectId);

                var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

                if (user is null || !user.IsActive)
                    throw new UserNotFoundException(request.UserId);

                var isActiveTeamMember = await teamRepository.HasActiveRoleAsync(project.TeamId, request.UserId,
                    [TeamRole.Owner, TeamRole.Admin, TeamRole.Member, TeamRole.Viewer], cancellationToken);

                if (!isActiveTeamMember)
                    throw new ForbiddenException("Only active team members can be added to a project.");

                var member = project.AddMember(request.UserId, request.ProjectRole, currentUser.UserId!.Value);

                memberId = member.Id;
            }, cancellationToken);

            return memberId;
        }
    }
}