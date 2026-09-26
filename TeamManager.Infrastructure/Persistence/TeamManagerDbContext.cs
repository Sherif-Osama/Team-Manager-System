using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Events;
using TeamManager.Domain.Common;
using TeamManager.Domain.Entities;
using TeamManager.Infrastructure.Persistence.Outbox;
namespace TeamManager.Infrastructure.Persistence
{

    public class TeamManagerDbContext(DbContextOptions<TeamManagerDbContext> options, IPublisher publisher)
        : DbContext(options), IUnitOfWork, IApplicationDbContext
    {
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
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            return ExecuteInTransactionAsync(action, IsolationLevel.ReadCommitted, cancellationToken);
        }

        private const int MaxDeadlockRetries = 3;

        public async Task ExecuteInSerializableTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            for (var attempt = 1; attempt <= MaxDeadlockRetries; attempt++)
            {
                try
                {
                    await ExecuteInTransactionAsync(action, IsolationLevel.Serializable, cancellationToken);

                    return;
                }
                catch (SqlException ex) when (ex.Number == 1205 && attempt < MaxDeadlockRetries)
                {
                    var delay = TimeSpan.FromMilliseconds(100 * attempt);

                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        private async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action,
            IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            await using var transaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);

            try
            {
                await action(cancellationToken);

                await SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var result = await base.SaveChangesAsync(cancellationToken);

            var aggregatesWithEvents = ChangeTracker.Entries<IHasDomainEvents>().Select(e => e.Entity)
                .Where(e => e.DomainEvents.Count > 0).ToList();

            var events = aggregatesWithEvents.SelectMany(e => e.DomainEvents).ToList();
            aggregatesWithEvents.ForEach(e => e.ClearDomainEvents());

            foreach (var domainEvent in events)
            {
                var wrapperType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                var notification = (INotification)Activator.CreateInstance(wrapperType, domainEvent)!;
                await publisher.Publish(notification, cancellationToken);
            }

            return result;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeamManagerDbContext).Assembly);
        }
    }
}
