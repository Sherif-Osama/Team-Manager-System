namespace TeamManager.Infrastructure.Communication
{
    public sealed class AppUrlOptions
    {
        public const string SectionName = "AppUrl";

        public string BaseUrl { get; init; } = string.Empty;
    }
}