namespace TeamManager.Infrastructure.Persistence.Outbox
{
    public sealed class OutboxMessage
    {
        public long Id { get; set; }
        public string Type { get; set; } = null!;
        public string Payload { get; set; } = null!;
        public DateTime OccurredOnUtc { get; set; }
        public DateTime? ProcessedOnUtc { get; set; }
        public int RetryCount { get; set; }
        public DateTime? NextAttemptOnUtc { get; set; }
        public string? Error { get; set; }
        public DateTime? FailedOnUtc { get; set; }
    }
}