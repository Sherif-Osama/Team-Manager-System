namespace TeamManager.Domain.Common
{
    public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents where TId : notnull
    {
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected AggregateRoot() { }
        protected AggregateRoot(TId id) : base(id) { }

        protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}