using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace TeamManager.Api.Extensions
{
    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var global = configuration.GetSection("RateLimiting:Global");

            var authentication = configuration.GetSection("RateLimiting:Authentication");

            var perIpLimit = global.GetValue("PerIpLimit", 300);

            var perIpWindow = TimeSpan.FromSeconds(global.GetValue("PerIpWindowSeconds", 60));

            var perUserLimit = global.GetValue("PerUserLimit", 120);
            var perUserWindow = TimeSpan.FromSeconds(global.GetValue("PerUserWindowSeconds", 60));

            RateLimitPartition<string> CreateSlidingWindowPartition(string partitionKey, int permitLimit, TimeSpan window)
            {
                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey,
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = window,
                        SegmentsPerWindow = 6,
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    });
            }

            string GetClientIp(HttpContext context)
            {
                return context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "unknown";
            }

            services.AddRateLimiter(options =>
            {

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, cancellationToken) =>
                {
                    var response = context.HttpContext.Response;
                    response.StatusCode = StatusCodes.Status429TooManyRequests;
                    response.ContentType = "application/problem+json";

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        response.Headers["Retry-After"] = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                    }

                    await response.WriteAsJsonAsync(
                        new ProblemDetails
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Title = "Too many requests",
                            Detail = "Request limit exceeded. Please try again later."
                        },
                        cancellationToken);
                };

                // Global IP limit: applies to every endpoint.
                var perIpLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                CreateSlidingWindowPartition(GetClientIp(context), perIpLimit, perIpWindow));

                // An additional limit for authenticated users.
                var perUserLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                        context =>
                        {
                            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                            context.User.FindFirst("sub")?.Value;

                            if (string.IsNullOrWhiteSpace(userId))
                            {
                                return RateLimitPartition.GetNoLimiter("anonymous");
                            }

                            return CreateSlidingWindowPartition($"user:{userId}", perUserLimit, perUserWindow);
                        });

                options.GlobalLimiter = PartitionedRateLimiter.CreateChained(perIpLimiter, perUserLimiter);

                // Stricter policies for authentication endpoints.
                options.AddPolicy("AuthLogin", context => CreatePolicyPartition(authentication, "Login", GetClientIp(context)));

                options.AddPolicy("AuthRegister", context => CreatePolicyPartition(authentication, "Register", GetClientIp(context)));

                options.AddPolicy("AuthRefresh", context => CreatePolicyPartition(authentication, "Refresh", GetClientIp(context)));

                options.AddPolicy("AuthForgotPassword", context => CreatePolicyPartition(authentication, "ForgotPassword",
                    GetClientIp(context)));

                options.AddPolicy("AuthResetPassword", context => CreatePolicyPartition(authentication, "ResetPassword",
                    GetClientIp(context)));
            });

            return services;

            RateLimitPartition<string> CreatePolicyPartition(IConfigurationSection section, string policyName, string partitionKey)
            {
                var policy = section.GetSection(policyName);
                var limit = policy.GetValue("PermitLimit", 5);
                var window = TimeSpan.FromSeconds(policy.GetValue("WindowSeconds", 60));

                return CreateSlidingWindowPartition(partitionKey, limit, window);
            }
        }
    }
}