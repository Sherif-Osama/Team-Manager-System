namespace TeamManager.Infrastructure.Communication
{
    public sealed class EmailOptions
    {
        public const string SectionName = "Email";
        public string Host { get; init; } = null!;
        public int Port { get; init; }
        public string Username { get; init; } = null!;
        public string Password { get; init; } = null!;
        public string FromName { get; init; } = null!;
        public string FromEmail { get; init; } = null!;
        public bool UseSsl { get; init; }
    }
}