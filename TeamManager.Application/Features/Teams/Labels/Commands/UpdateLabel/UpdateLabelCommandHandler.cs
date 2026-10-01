using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TeamExceptions;

namespace TeamManager.Application.Features.Teams.Labels.Commands.UpdateLabel
{
    public sealed class UpdateLabelCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateLabelCommand>
    {
        public async Task Handle(UpdateLabelCommand request, CancellationToken cancellationToken)
        {
            var team = await teamRepository.GetByIdWithLabelsAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            team.UpdateLabel(request.LabelId, request.Name, request.ColorHex);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}