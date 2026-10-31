using Moq;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Application.Common.Events;
using TeamManager.Application.Common.Events.EventHandlers.Caching;
using TeamManager.Domain.Common.Events.Invitations;
using TeamManager.Domain.Common.Events.Projects;
using TeamManager.Domain.Common.Events.Roles;
using TeamManager.Domain.Common.Events.Teams;
using TeamManager.Domain.Common.Events.Users;
using TeamManager.Domain.Enums;

namespace TeamManager.Tests.UnitTest.Application.EventHandlers;

public sealed class CachingHandlersTests
{
    private readonly Mock<IPostCommitCacheInvalidationDispatcher> _dispatcher = new();

    [Fact]
    public async Task OnUserRoleAssigned_EnqueuesUserPermissionsTag()
    {
        var handler = new InvalidateUserPermissionsCacheOnUserRoleAssignedHandler(_dispatcher.Object);
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<UserRoleAssignedDomainEvent>(
            new UserRoleAssignedDomainEvent(userId, 2)), CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserPermissions(userId)), Times.Once);
    }

    [Fact]
    public async Task OnUserRoleRevoked_EnqueuesUserPermissionsTag()
    {
        var handler = new InvalidateUserPermissionsCacheOnUserRoleRevokedHandler(_dispatcher.Object);
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<UserRoleRevokedDomainEvent>(
            new UserRoleRevokedDomainEvent(userId, 2)), CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserPermissions(userId)), Times.Once);
    }

    [Fact]
    public async Task OnUserDeactivated_EnqueuesPermissionsAndAuthorizationTags()
    {
        var handler = new InvalidateUserCacheOnUserDeactivatedHandler(_dispatcher.Object);
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<UserDeactivatedDomainEvent>(
            new UserDeactivatedDomainEvent(userId, "u@example.com")), CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserPermissions(userId)), Times.Once);
        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserAuthorization(userId)), Times.Once);
    }

    [Fact]
    public async Task OnUserDeleted_EnqueuesPermissionsAndAuthorizationTags()
    {
        var handler = new InvalidateUserCacheOnUserDeletedHandler(_dispatcher.Object);
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<UserDeletedDomainEvent>(
            new UserDeletedDomainEvent(userId, "u@example.com")), CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserPermissions(userId)), Times.Once);
        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserAuthorization(userId)), Times.Once);
    }

    [Fact]
    public async Task OnUserReactivated_EnqueuesPermissionsAndAuthorizationTags()
    {
        var handler = new InvalidateUserCacheOnUserReactivatedHandler(_dispatcher.Object);
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<UserReactivatedDomainEvent>(
            new UserReactivatedDomainEvent(userId, "u@example.com")), CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserPermissions(userId)), Times.Once);
        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserAuthorization(userId)), Times.Once);
    }

    [Fact]
    public async Task OnTeamLabelsChanged_EnqueuesTeamLabelsTag()
    {
        var handler = new InvalidateTeamLabelsCacheHandler(_dispatcher.Object);
        var teamId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<TeamLabelsChangedDomainEvent>(
            new TeamLabelsChangedDomainEvent(teamId)), CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.TeamLabels(teamId)), Times.Once);
    }

    [Fact]
    public async Task OnRolePermissionAdded_EnqueuesAllUserPermissionsTag()
    {
        var handler = new InvalidateAllUserPermissionsCacheOnRolePermissionAddedHandler(_dispatcher.Object);

        await handler.Handle(new DomainEventNotification<RolePermissionAddedDomainEvent>(
            new RolePermissionAddedDomainEvent(1, 3)), CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.AllUserPermissions), Times.Once);
    }

    [Fact]
    public async Task OnRolePermissionRemoved_EnqueuesAllUserPermissionsTag()
    {
        var handler = new InvalidateAllUserPermissionsCacheOnRolePermissionRemovedHandler(_dispatcher.Object);

        await handler.Handle(new DomainEventNotification<RolePermissionRemovedDomainEvent>(
            new RolePermissionRemovedDomainEvent(1, 3)), CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.AllUserPermissions), Times.Once);
    }

    [Fact]
    public async Task OnTeamMemberRemoved_EnqueuesUserAuthorizationTag()
    {
        var handler = new InvalidateUserAuthorizationCacheOnTeamMemberRemovedHandler(_dispatcher.Object);
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<TeamMemberRemovedDomainEvent>(
            new TeamMemberRemovedDomainEvent(teamId, 1, userId, Guid.NewGuid())), CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserAuthorization(userId)), Times.Once);
    }

    [Fact]
    public async Task OnTeamMemberRoleChanged_EnqueuesUserAuthorizationTag()
    {
        var handler = new InvalidateUserAuthorizationCacheOnTeamMemberRoleChangedHandler(_dispatcher.Object);
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<TeamMemberRoleChangedDomainEvent>(
            new TeamMemberRoleChangedDomainEvent(teamId, 1, userId, Guid.NewGuid(), TeamRole.Member, TeamRole.Admin)),
            CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserAuthorization(userId)), Times.Once);
    }

    [Fact]
    public async Task OnTeamInvitationAccepted_EnqueuesUserAuthorizationTag()
    {
        var handler = new InvalidateUserAuthorizationCacheOnTeamInvitationAcceptedHandler(_dispatcher.Object);
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<TeamInvitationAcceptedDomainEvent>(
            new TeamInvitationAcceptedDomainEvent(teamId, Guid.NewGuid(), userId, "u@example.com", TeamRole.Member)),
            CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserAuthorization(userId)), Times.Once);
    }

    [Fact]
    public async Task OnTeamOwnershipTransferred_EnqueuesTeamAndBothUserTags()
    {
        var handler = new InvalidateAuthorizationCacheOnTeamOwnershipTransferredHandler(_dispatcher.Object);
        var teamId = Guid.NewGuid();
        var previous = Guid.NewGuid();
        var next = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<TeamOwnershipTransferredDomainEvent>(
            new TeamOwnershipTransferredDomainEvent(teamId, previous, next, previous)), CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.TeamAuthorization(teamId)), Times.Once);
        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserAuthorization(previous)), Times.Once);
        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserAuthorization(next)), Times.Once);
    }

    [Fact]
    public async Task OnProjectMemberRemoved_EnqueuesUserAuthorizationTag()
    {
        var handler = new InvalidateUserAuthorizationCacheOnProjectMemberRemovedHandler(_dispatcher.Object);
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<ProjectMemberRemovedDomainEvent>(
            new ProjectMemberRemovedDomainEvent(projectId, 1, userId, Guid.NewGuid())), CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserAuthorization(userId)), Times.Once);
    }

    [Fact]
    public async Task OnProjectMemberRoleChanged_EnqueuesUserAuthorizationTag()
    {
        var handler = new InvalidateUserAuthorizationCacheOnProjectMemberRoleChangedHandler(_dispatcher.Object);
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<ProjectMemberRoleChangedDomainEvent>(
            new ProjectMemberRoleChangedDomainEvent(projectId, 1, userId, Guid.NewGuid(), ProjectRole.Member, ProjectRole.Admin)),
            CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserAuthorization(userId)), Times.Once);
    }

    [Fact]
    public async Task OnProjectOwnershipTransferred_EnqueuesProjectAndBothUserTags()
    {
        var handler = new InvalidateAuthorizationCacheOnProjectOwnershipTransferredHandler(_dispatcher.Object);
        var projectId = Guid.NewGuid();
        var previous = Guid.NewGuid();
        var next = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<ProjectOwnershipTransferredDomainEvent>(
            new ProjectOwnershipTransferredDomainEvent(projectId, previous, next, previous)), CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.ProjectAuthorization(projectId)), Times.Once);
        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserAuthorization(previous)), Times.Once);
        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserAuthorization(next)), Times.Once);
    }

    [Fact]
    public async Task OnTeamMemberAdded_EnqueuesUserAuthorizationTag()
    {
        var handler = new InvalidateUserAuthorizationCacheOnTeamMemberAddedHandler(_dispatcher.Object);
        var team = new global::TeamManager.Domain.Entities.Team(Guid.NewGuid(), "T1", Guid.NewGuid(), Guid.NewGuid());
        var member = team.AddMember(Guid.NewGuid(), TeamRole.Member);

        await handler.Handle(new DomainEventNotification<TeamMemberAddedDomainEvent>(
            new TeamMemberAddedDomainEvent(member)), CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserAuthorization(member.UserId)), Times.Once);
    }

    [Fact]
    public async Task OnProjectMemberAdded_EnqueuesUserAuthorizationTag()
    {
        var handler = new InvalidateUserAuthorizationCacheOnProjectMemberAddedHandler(_dispatcher.Object);
        var project = new global::TeamManager.Domain.Entities.Project(Guid.NewGuid(), Guid.NewGuid(), "P1", Guid.NewGuid());
        var member = project.AddMember(Guid.NewGuid(), ProjectRole.Member);

        await handler.Handle(new DomainEventNotification<ProjectMemberAddedDomainEvent>(
            new ProjectMemberAddedDomainEvent(member)), CancellationToken.None);

        _dispatcher.Verify(x => x.EnqueueTag(CacheTags.UserAuthorization(member.UserId)), Times.Once);
    }
}
