using Microsoft.EntityFrameworkCore;
using TeamManager.Domain.Entities;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface IApplicationDbContext
    {
        DbSet<Team> Teams { get; }
        DbSet<TeamMember> TeamMembers { get; }
        DbSet<TeamInvitation> TeamInvitations { get; }
        DbSet<User> Users { get; }
        DbSet<Role> Roles { get; }
        DbSet<UserRole> UserRoles { get; }
        DbSet<Project> Projects { get; }
        DbSet<ProjectMember> ProjectMembers { get; }
        DbSet<TaskItem> Tasks { get; }
        DbSet<TaskDependency> TaskDependencies { get; }
        DbSet<TaskChecklistItem> TaskChecklistItems { get; }
        DbSet<Label> Labels { get; }
        DbSet<TaskAttachment> TaskAttachments { get; }
        DbSet<Notification> Notifications { get; }
        DbSet<NotificationPreference> NotificationPreferences { get; }
        DbSet<ActivityLog> ActivityLogs { get; }
    }
}