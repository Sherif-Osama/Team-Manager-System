using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.AddMember
{
    public sealed class AddMemberCommandHandler(ITeamRepository teamRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<AddMemberCommand, long>
    {

        public async Task<long> Handle(AddMemberCommand request, CancellationToken cancellationToken)
        {
            var team = await teamRepository.GetByIdWithMembersAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

            if (user is null || !user.IsActive)
                throw new UserNotFoundException(request.UserId);

            var member = team.AddMember(request.UserId, request.TeamRole);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return member.Id;
        }
    }
}