using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages;

namespace TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessagesJob
{
    public sealed class ProcessOutboxMessagesJob(IServiceScopeFactory scopeFactory,
        ILogger<ProcessOutboxMessagesJob> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();

                    var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessorService>();

                    await processor.ProcessAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while processing outbox messages.");
                }
            }
        }
    }
}
