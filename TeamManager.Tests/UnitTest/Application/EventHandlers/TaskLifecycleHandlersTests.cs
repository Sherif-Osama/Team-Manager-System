using Moq;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Events;
using TeamManager.Application.Common.Events.EventHandlers.TaskLifecycle;
using TeamManager.Domain.Common.Events.Projects;
using TeamManager.Domain.Common.Events.Teams;
using TeamManager.Domain.Common.Events.Users;

namespace TeamManager.Tests.UnitTest.Application.EventHandlers;

public sealed class TaskLifecycleHandlersTests
{
    private readonly Mock<ITaskRepository> _taskRepository = new();

    [Fact]
    public async Task UnassignTasksOnProjectMemberRemovedHandler_UnassignsTasksInProject()
    {
        var handler = new UnassignTasksOnProjectMemberRemovedHandler(_taskRepository.Object);
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<ProjectMemberRemovedDomainEvent>(
            new ProjectMemberRemovedDomainEvent(projectId, 1, userId, Guid.NewGuid())), CancellationToken.None);

        _taskRepository.Verify(x => x.UnassignActiveTasksByProjectAsync(projectId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnassignTasksOnTeamMemberRemovedHandler_UnassignsTasksInTeam()
    {
        var handler = new UnassignTasksOnTeamMemberRemovedHandler(_taskRepository.Object);
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<TeamMemberRemovedDomainEvent>(
            new TeamMemberRemovedDomainEvent(teamId, 1, userId, Guid.NewGuid())), CancellationToken.None);

        _taskRepository.Verify(x => x.UnassignActiveTasksByTeamAsync(teamId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnassignTasksOnUserDeactivatedHandler_UnassignsAllActiveTasks()
    {
        var handler = new UnassignTasksOnUserDeactivatedHandler(_taskRepository.Object);
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<UserDeactivatedDomainEvent>(
            new UserDeactivatedDomainEvent(userId, "u@example.com")), CancellationToken.None);

        _taskRepository.Verify(x => x.UnassignActiveTasksAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnassignTasksOnUserDeletedHandler_UnassignsAllActiveTasks()
    {
        var handler = new UnassignTasksOnUserDeletedHandler(_taskRepository.Object);
        var userId = Guid.NewGuid();

        await handler.Handle(new DomainEventNotification<UserDeletedDomainEvent>(
            new UserDeletedDomainEvent(userId, "u@example.com")), CancellationToken.None);

        _taskRepository.Verify(x => x.UnassignActiveTasksAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
