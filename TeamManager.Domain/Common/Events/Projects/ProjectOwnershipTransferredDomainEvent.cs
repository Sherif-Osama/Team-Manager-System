namespace TeamManager.Domain.Common.Events.Projects
{
    public sealed record ProjectOwnershipTransferredDomainEvent(Guid ProjectId, Guid PreviousOwnerUserId,
        Guid NewOwnerUserId, Guid ActorUserId) : IDomainEvent;
}