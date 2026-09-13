namespace TeamManager.Application.Features.Projects.Project.Commands.TransferProjectOwnership
{
    public sealed record TransferProjectOwnershipRequest(Guid NewOwnerUserId);
}