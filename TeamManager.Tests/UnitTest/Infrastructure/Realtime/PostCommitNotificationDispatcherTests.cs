using Moq;
using TeamManager.Application.Abstractions.Realtime;
using TeamManager.Application.Features.Notifications.Realtime;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Services.Realtime;

namespace TeamManager.Tests.UnitTest.Infrastructure.Realtime;

public sealed class PostCommitNotificationDispatcherTests
{
    private readonly Mock<IRealtimeNotifier> _realtimeNotifier = new();
    private readonly PostCommitNotificationDispatcher _sut;

    public PostCommitNotificationDispatcherTests()
    {
        _sut = new(_realtimeNotifier.Object);
    }

    private static Notification CreateNotification(Guid recipient) =>
        new(recipient, NotificationType.TaskAssigned, "Task Assigned");

    [Fact]
    public async Task DispatchAsync_SendsEachEnqueuedNotificationToItsRecipient()
    {
        var first = CreateNotification(Guid.NewGuid());
        var second = CreateNotification(Guid.NewGuid());
        _sut.Enqueue(first);
        _sut.Enqueue(second);

        await _sut.DispatchAsync(CancellationToken.None);

        _realtimeNotifier.Verify(x => x.NotifyUserAsync(first.RecipientUserId,
            It.IsAny<NotificationRealtimePayload>(), It.IsAny<CancellationToken>()), Times.Once);
        _realtimeNotifier.Verify(x => x.NotifyUserAsync(second.RecipientUserId,
            It.IsAny<NotificationRealtimePayload>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WhenNotifierFails_ContinuesWithRemainingNotifications()
    {
        var failing = CreateNotification(Guid.NewGuid());
        var working = CreateNotification(Guid.NewGuid());
        _realtimeNotifier.Setup(x => x.NotifyUserAsync(failing.RecipientUserId,
                It.IsAny<NotificationRealtimePayload>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());
        _sut.Enqueue(failing);
        _sut.Enqueue(working);

        await _sut.DispatchAsync(CancellationToken.None);

        _realtimeNotifier.Verify(x => x.NotifyUserAsync(working.RecipientUserId,
            It.IsAny<NotificationRealtimePayload>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WithNoPendingNotifications_DoesNothing()
    {
        await _sut.DispatchAsync(CancellationToken.None);

        _realtimeNotifier.Verify(x => x.NotifyUserAsync(It.IsAny<Guid>(),
            It.IsAny<NotificationRealtimePayload>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_AfterDispatch_PendingNotificationsAreCleared()
    {
        var notification = CreateNotification(Guid.NewGuid());
        _sut.Enqueue(notification);

        await _sut.DispatchAsync(CancellationToken.None);
        await _sut.DispatchAsync(CancellationToken.None);

        _realtimeNotifier.Verify(x => x.NotifyUserAsync(It.IsAny<Guid>(),
            It.IsAny<NotificationRealtimePayload>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Discard_ClearsNotificationsWithoutDispatching()
    {
        _sut.Enqueue(CreateNotification(Guid.NewGuid()));

        _sut.Discard();
        await _sut.DispatchAsync(CancellationToken.None);

        _realtimeNotifier.Verify(x => x.NotifyUserAsync(It.IsAny<Guid>(),
            It.IsAny<NotificationRealtimePayload>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
