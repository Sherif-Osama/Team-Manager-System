namespace TeamManager.Domain.Common.Events
{
    public sealed record TeamOwnershipTransferredDomainEvent(Guid TeamId, Guid PreviousOwnerUserId, Guid NewOwnerUserId,
        Guid ActorUserId) : IDomainEvent;
}