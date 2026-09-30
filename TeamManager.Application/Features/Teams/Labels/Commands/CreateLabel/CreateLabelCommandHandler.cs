using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TeamExceptions;

namespace TeamManager.Application.Features.Teams.Labels.Commands.CreateLabel
{
    public sealed class CreateLabelCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<CreateLabelCommand, long>
    {
        public async Task<long> Handle(CreateLabelCommand request, CancellationToken cancellationToken)
        {

            var team = await teamRepository.GetByIdWithLabelsAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            var label = team.AddLabel(request.Name, request.ColorHex);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return label.Id;
        }
    }
}