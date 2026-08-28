using MediatR;

namespace TeamManager.Application.Features.Teams.Commands.TransferOwnership
{
    public sealed record TransferOwnershipCommand(Guid TeamId, Guid NewOwnerUserId) : IRequest;
}
