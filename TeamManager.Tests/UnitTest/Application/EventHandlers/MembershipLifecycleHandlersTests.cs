using Moq;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Events;
using TeamManager.Application.Common.Events.EventHandlers.MembershipLifecycle;
using TeamManager.Domain.Common.Events.Teams;
using TeamManager.Domain.Common.Events.Users;

namespace TeamManager.Tests.UnitTest.Application.EventHandlers;

public sealed class MembershipLifecycleHandlersTests
{
    private readonly Mock<ITeamRepository> _teamRepository = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();

    [Fact]
    public async Task ReactivateProjectMembershipsHandler_OnUserReactivated_ReactivatesProjectMemberships()
    {
        var handler = new ReactivateProjectMembershipsOnUserReactivatedHandler(_projectRepository.Object);
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<UserReactivatedDomainEvent>(
            new UserReactivatedDomainEvent(userId, "u@example.com")), CancellationToken.None);

        _projectRepository.Verify(x => x.ReactivateSuspendedMembershipsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReactivateTeamMembershipsHandler_OnUserReactivated_ReactivatesTeamMemberships()
    {
        var handler = new ReactivateTeamMembershipsOnUserReactivatedHandler(_teamRepository.Object);
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<UserReactivatedDomainEvent>(
            new UserReactivatedDomainEvent(userId, "u@example.com")), CancellationToken.None);

        _teamRepository.Verify(x => x.ReactivateSuspendedMembershipsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveProjectMembershipsHandler_OnTeamMemberRemoved_RemovesMembershipsByTeam()
    {
        var handler = new RemoveProjectMembershipsOnTeamMemberRemovedHandler(_projectRepository.Object);
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<TeamMemberRemovedDomainEvent>(
            new TeamMemberRemovedDomainEvent(teamId, 1, userId, Guid.NewGuid())), CancellationToken.None);

        _projectRepository.Verify(x => x.RemoveMembershipsByTeamAsync(teamId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveProjectMembershipsHandler_OnUserDeleted_RemovesActiveProjectMemberships()
    {
        var handler = new RemoveProjectMembershipsOnUserDeletedHandler(_projectRepository.Object);
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<UserDeletedDomainEvent>(
            new UserDeletedDomainEvent(userId, "u@example.com")), CancellationToken.None);

        _projectRepository.Verify(x => x.RemoveActiveMembershipsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTeamMembershipsHandler_OnUserDeleted_RemovesActiveTeamMemberships()
    {
        var handler = new RemoveTeamMembershipsOnUserDeletedHandler(_teamRepository.Object);
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<UserDeletedDomainEvent>(
            new UserDeletedDomainEvent(userId, "u@example.com")), CancellationToken.None);

        _teamRepository.Verify(x => x.RemoveActiveMembershipsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SuspendProjectMembershipsHandler_OnUserDeactivated_DeactivatesOwnedProjectsAndSuspendsMemberships()
    {
        var handler = new SuspendProjectMembershipsOnUserDeactivatedHandler(_projectRepository.Object);
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<UserDeactivatedDomainEvent>(
            new UserDeactivatedDomainEvent(userId, "u@example.com")), CancellationToken.None);

        _projectRepository.Verify(x => x.DeactivateOwnedProjectsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _projectRepository.Verify(x => x.SuspendActiveMembershipsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SuspendTeamMembershipsHandler_OnUserDeactivated_DeactivatesOwnedTeamsAndSuspendsMemberships()
    {
        var handler = new SuspendTeamMembershipsOnUserDeactivatedHandler(_teamRepository.Object);
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<UserDeactivatedDomainEvent>(
            new UserDeactivatedDomainEvent(userId, "u@example.com")), CancellationToken.None);

        _teamRepository.Verify(x => x.DeactivateOwnedTeamsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _teamRepository.Verify(x => x.SuspendActiveMembershipsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
