namespace TeamManager.Domain.Common.Events.Teams
{
    public sealed record TeamOwnershipTransferredDomainEvent(Guid TeamId, Guid PreviousOwnerUserId, Guid NewOwnerUserId,
        Guid ActorUserId) : IDomainEvent;
}