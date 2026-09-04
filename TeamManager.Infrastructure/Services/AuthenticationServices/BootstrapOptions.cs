namespace TeamManager.Infrastructure.Services.AuthenticationServices
{
    public sealed class BootstrapOptions
    {
        public const string SectionName = "Bootstrap";

        public string AdminSecret { get; set; } = string.Empty;
    }
}