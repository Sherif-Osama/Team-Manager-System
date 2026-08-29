using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Team.Commands.CreateTeam
{
    public sealed class CreateTeamCommandHandler(ICurrentUser currentUser, IUnitOfWork unitOfWork, ITeamRepository teamRepository)
        : IRequestHandler<CreateTeamCommand, Guid>
    {

        public async Task<Guid> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");


            var userId = currentUser.UserId.Value;

            var existingTeam = await teamRepository.GetByNameAsync(request.Name, cancellationToken);

            if (existingTeam is not null)
                throw new TeamNameAlreadyExistsException(request.Name);

            var team = new Domain.Entities.Team(Guid.NewGuid(), request.Name, userId, userId, request.Description);

            await teamRepository.AddAsync(team, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return team.Id;
        }
    }
}