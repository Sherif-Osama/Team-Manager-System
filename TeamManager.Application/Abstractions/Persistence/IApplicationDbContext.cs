using Microsoft.EntityFrameworkCore;
using TeamManager.Domain.Entities;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface IApplicationDbContext
    {
        DbSet<Team> Teams { get; }
        DbSet<TeamMember> TeamMembers { get; }
        DbSet<TeamInvitation> TeamInvitations { get; }
    }
}