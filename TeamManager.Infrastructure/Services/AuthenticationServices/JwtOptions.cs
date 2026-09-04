namespace TeamManager.Infrastructure.Services.AuthenticationServices
{
    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";
        public string Key { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int ExpirationMinutes { get; set; }

        public int RefreshTokenExpirationDays { get; set; }
    }
}
