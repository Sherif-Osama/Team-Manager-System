using TeamManager.Api.Authentication;
using TeamManager.Api.Extensions;
using TeamManager.Api.Hubs;
using TeamManager.Api.Middleware;
using TeamManager.Application;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Realtime;
using TeamManager.Infrastructure;

namespace TeamManager.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddApiRateLimiting(builder.Configuration);
            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddJwtAuthentication(builder.Configuration);
            builder.Services.AddApiCors(builder.Configuration);
            // Add services to the container.
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<ICurrentUser, CurrentUser>();

            var redis = builder.Configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(redis))
                builder.Services.AddSignalR();
            else
                builder.Services.AddSignalR().AddStackExchangeRedis(redis);

            builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerDocumentation();
            builder.Services.AddHealthChecks();
            var app = builder.Build();
            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(CorsExtensions.FrontendPolicy);

            app.UseMiddleware<SecurityEventLoggingMiddleware>();
            app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

            app.UseAuthentication();

            app.UseRateLimiter();

            app.UseAuthorization();

            app.MapControllers();

            app.MapHealthChecks("/health").AllowAnonymous();
            app.MapHub<NotificationsHub>("/hubs/notifications");
            app.Run();
        }
    }
}
