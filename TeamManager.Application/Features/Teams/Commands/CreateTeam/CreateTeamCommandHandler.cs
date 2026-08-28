using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Domain.Entities;

namespace TeamManager.Application.Features.Teams.Commands.CreateTeam
{
    public sealed class CreateTeamCommandHandler : IRequestHandler<CreateTeamCommand, Guid>
    {
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ITeamRepository _teamRepository;

        public CreateTeamCommandHandler(ICurrentUser currentUser, IUnitOfWork unitOfWork, ITeamRepository teamRepository)
        {
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _teamRepository = teamRepository;
        }

        public async Task<Guid> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var userId = _currentUser.UserId.Value;

            var existingTeam = await _teamRepository.GetByNameAsync(
                request.Name,
                cancellationToken);

            if (existingTeam is not null)
                throw new TeamNameAlreadyExistsException(request.Name);

            var team = new Team(Guid.NewGuid(), request.Name, userId, userId, request.Description);

            await _teamRepository.AddAsync(team, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return team.Id;
        }
    }
}