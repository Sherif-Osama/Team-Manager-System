using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TeamManager.Infrastructure.BackgroundJobs.RefreshTokenCleanup
{
    public sealed class RefreshTokenCleanupJob(IServiceScopeFactory scopeFactory, ILogger<RefreshTokenCleanupJob> logger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromDays(30));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();

                        var cleanupService = scope.ServiceProvider.GetRequiredService<ExpiredRefreshTokenCleanupService>();

                        var deletedCount = await cleanupService.CleanupAsync(stoppingToken);

                        if (deletedCount > 0)
                            logger.LogInformation("Deleted {DeletedCount} expired refresh token records.", deletedCount);

                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred while cleaning up expired refresh tokens.");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected when the application is stopping.
            }
        }
    }
}