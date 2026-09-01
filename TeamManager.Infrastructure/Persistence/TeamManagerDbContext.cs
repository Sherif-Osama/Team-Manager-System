using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence
{

    public class TeamManagerDbContext : DbContext, IUnitOfWork, IApplicationDbContext
    {
        public TeamManagerDbContext(DbContextOptions<TeamManagerDbContext> options) : base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<TeamInvitation> TeamInvitations => Set<TeamInvitation>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
        public DbSet<TaskItem> Tasks => Set<TaskItem>();
        public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
        public DbSet<Label> Labels => Set<Label>();
        public DbSet<TaskLabel> TaskLabels => Set<TaskLabel>();
        public DbSet<TaskChecklistItem> TaskChecklistItems => Set<TaskChecklistItem>();
        public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();
        public DbSet<TaskComment> TaskComments => Set<TaskComment>();
        public DbSet<CommentMention> CommentMentions => Set<CommentMention>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeamManagerDbContext).Assembly);
        }
    }
}