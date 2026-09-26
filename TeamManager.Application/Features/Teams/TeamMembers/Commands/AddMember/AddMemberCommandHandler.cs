using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TeamExceptions;
using TeamManager.Application.Common.Exceptions.UserExceptions;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.AddMember
{
    public sealed class AddMemberCommandHandler(ITeamRepository teamRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<AddMemberCommand, long>
    {
        public async Task<long> Handle(AddMemberCommand request, CancellationToken cancellationToken)
        {
            Domain.Entities.TeamMember? member = null;

            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var team = await teamRepository.GetByIdWithMembersAsync(request.TeamId, ct);
                if (team is null)
                    throw new TeamNotFoundException(request.TeamId);

                var user = await userRepository.GetByIdAsync(request.UserId, ct);

                if (user is null || !user.IsActive)
                    throw new UserNotFoundException(request.UserId);

                member = team.AddMember(request.UserId, request.TeamRole);
            }, cancellationToken);

            return member?.Id ?? default;
        }
    }
}