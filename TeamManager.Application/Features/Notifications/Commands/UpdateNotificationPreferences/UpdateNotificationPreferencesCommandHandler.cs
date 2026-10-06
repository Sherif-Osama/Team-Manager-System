using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Entities;

namespace TeamManager.Application.Features.Notifications.Commands.UpdateNotificationPreferences
{
    public sealed class UpdateNotificationPreferencesCommandHandler(INotificationRepository notificationRepository,
        ICurrentUser currentUser, IUnitOfWork unitOfWork) : IRequestHandler<UpdateNotificationPreferencesCommand>
    {
        public async Task Handle(UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            var existingPreferences = await notificationRepository.GetPreferencesByUserIdAsync(userId, cancellationToken);

            foreach (var item in request.Preferences)
            {
                var preference = existingPreferences.FirstOrDefault(x => x.NotificationType == item.NotificationType);

                if (preference is not null)
                {
                    if (item.IsEnabled)
                        preference.Enable();
                    else
                        preference.Disable();
                }
                else
                {
                    await notificationRepository.AddPreferenceAsync(new NotificationPreference(userId, item.NotificationType, item.IsEnabled), cancellationToken);
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}