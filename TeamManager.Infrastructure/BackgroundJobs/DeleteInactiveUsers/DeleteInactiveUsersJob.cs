using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TeamManager.Infrastructure.BackgroundJobs.DeleteInactiveUsers
{
    public sealed class DeleteInactiveUsersJob(IServiceScopeFactory scopeFactory, ILogger<DeleteInactiveUsersJob> logger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromDays(1));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var deletionService = scope.ServiceProvider.GetRequiredService<InactiveUserDeletionService>();
                    await deletionService.DeleteInactiveUsersAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                { break; }
                catch (Exception ex)
                { logger.LogError(ex, "An error occurred while deleting inactive users."); }
            }
        }
    }
}