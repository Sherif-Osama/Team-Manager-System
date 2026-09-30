using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TeamExceptions;

namespace TeamManager.Application.Features.Teams.Labels.Commands.DeleteLabel
{
    public sealed class DeleteLabelCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<DeleteLabelCommand>
    {
        public async Task Handle(DeleteLabelCommand request, CancellationToken cancellationToken)
        {
            var team = await teamRepository.GetByIdWithLabelsAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            var label = team.RemoveLabel(request.LabelId);

            teamRepository.DeleteLabel(label);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}