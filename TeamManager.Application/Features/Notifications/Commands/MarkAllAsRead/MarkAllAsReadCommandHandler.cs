using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Notifications.Commands.MarkAllAsRead
{
    public sealed class MarkAllAsReadCommandHandler(INotificationRepository notificationRepository, ICurrentUser currentUser,
        IUnitOfWork unitOfWork) : IRequestHandler<MarkAllAsReadCommand>
    {
        public async Task Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");


            var notifications = await notificationRepository.GetUnreadByRecipientIdAsync(currentUser.UserId.Value, cancellationToken);

            foreach (var notification in notifications)
                notification.MarkAsRead();


            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}