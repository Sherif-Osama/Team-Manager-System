using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Commands.SoftDeleteTeam
{
    public sealed class SoftDeleteTeamCommandHandler : IRequestHandler<SoftDeleteTeamCommand>
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SoftDeleteTeamCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
        {
            _teamRepository = teamRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(SoftDeleteTeamCommand request, CancellationToken cancellationToken)
        {
            var team = await _teamRepository.GetByIdForUpdateAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            team.SoftDelete();

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}