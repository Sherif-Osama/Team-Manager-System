using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.NotificationExceptions;

namespace TeamManager.Application.Features.Notifications.Commands.MarkAsRead
{
    public sealed class MarkAsReadCommandHandler(INotificationRepository notificationRepository, ICurrentUser currentUser,
        IUnitOfWork unitOfWork) : IRequestHandler<MarkAsReadCommand>
    {
        public async Task Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);

            if (notification is null || notification.RecipientUserId != currentUser.UserId.Value)
                throw new NotificationNotFoundException(request.NotificationId);

            notification.MarkAsRead();

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}