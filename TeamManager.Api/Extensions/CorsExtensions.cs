namespace TeamManager.Api.Extensions
{
    public static class CorsExtensions
    {
        public const string FrontendPolicy = "Frontend";

        public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [];

            if (origins.Length == 0)
            {
                throw new InvalidOperationException("At least one CORS origin must be configured.");
            }

            foreach (var origin in origins)
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp
                    && uri.Scheme != Uri.UriSchemeHttps) || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query)
                    || !string.IsNullOrEmpty(uri.Fragment) || origin.Contains('*'))
                {
                    throw new InvalidOperationException($"Invalid CORS origin: '{origin}'. " + "Use scheme://host[:port] without a path.");
                }
            }

            services.AddCors(options =>
            {
                options.AddPolicy(FrontendPolicy, policy =>
                {
                    policy.WithOrigins(origins).WithMethods("GET", "POST", "PUT", "DELETE").AllowAnyHeader().AllowCredentials();
                });
            });

            return services;
        }
    }
}