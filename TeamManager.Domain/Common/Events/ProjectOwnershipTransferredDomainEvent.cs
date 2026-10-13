namespace TeamManager.Domain.Common.Events
{
    public sealed record ProjectOwnershipTransferredDomainEvent(Guid ProjectId, Guid PreviousOwnerUserId,
        Guid NewOwnerUserId, Guid ActorUserId) : IDomainEvent;
}