using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TeamManager.Infrastructure.BackgroundJobs.OutboxRetention
{
    public sealed class OutboxRetentionCleanupJob(IServiceScopeFactory scopeFactory,
        ILogger<OutboxRetentionCleanupJob> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await CleanupAsync(stoppingToken);

                using var timer = new PeriodicTimer(TimeSpan.FromDays(1));

                while (await timer.WaitForNextTickAsync(stoppingToken))
                    await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected when the application is stopping.
            }
        }

        private async Task CleanupAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<OutboxRetentionCleanupService>();
                var deletedCount = await cleanupService.CleanupAsync(stoppingToken);

                if (deletedCount > 0)
                    logger.LogInformation("Deleted {DeletedCount} expired outbox messages.", deletedCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An error occurred while deleting expired outbox messages.");
            }
        }
    }
}
