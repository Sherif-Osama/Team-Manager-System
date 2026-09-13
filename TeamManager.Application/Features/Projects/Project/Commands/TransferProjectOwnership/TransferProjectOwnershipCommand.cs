using MediatR;

namespace TeamManager.Application.Features.Projects.Project.Commands.TransferProjectOwnership
{
    public sealed record TransferProjectOwnershipCommand(Guid ProjectId, Guid NewOwnerUserId) : IRequest;
}