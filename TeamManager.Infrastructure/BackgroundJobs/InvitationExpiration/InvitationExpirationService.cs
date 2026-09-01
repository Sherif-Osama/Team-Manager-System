using Microsoft.EntityFrameworkCore;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;

namespace TeamManager.Infrastructure.BackgroundJobs.InvitationExpiration
{
    public sealed class InvitationExpirationService(TeamManagerDbContext context)
    {
        public async Task ExpireAsync(CancellationToken cancellationToken)
        {
            var invitations = await context.TeamInvitations.Where(x => x.Status == TeamInvitationStatus.Pending
            && x.ExpiresAtUtc <= DateTime.UtcNow).ToListAsync(cancellationToken);

            foreach (var invitation in invitations)
            {
                invitation.MarkExpired();
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}