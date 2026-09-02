using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TeamManager.Infrastructure.BackgroundJobs.InvitationExpiration
{
    public sealed class ExpirePendingInvitationsJob(IServiceScopeFactory scopeFactory,
        ILogger<ExpirePendingInvitationsJob> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();

                    var expirationService = scope.ServiceProvider.GetRequiredService<InvitationExpirationService>();

                    await expirationService.ExpireAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                { break; }
                catch (Exception ex)
                { logger.LogError(ex, "An error occurred while processing expired invitations."); }
            }
        }
    }
}