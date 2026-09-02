namespace TeamManager.Infrastructure.Communication
{
    public interface IEmailSender
    {
        Task SendInvitationEmailAsync(string to, string invitationToken, CancellationToken cancellationToken);
        Task SendEmailConfirmationAsync(string to, string confirmationToken, CancellationToken cancellationToken);
        Task SendPasswordChangedNotificationAsync(string to, CancellationToken cancellationToken);
        Task SendAccountDeletedAsync(string to, DateTime deletedAtUtc, string? deviceInfo, CancellationToken cancellationToken);
        Task SendAccountDeactivatedAsync(string to, DateTime deactivatedAtUtc, string? deviceInfo, CancellationToken cancellationToken);
        Task SendAccountActivatedAsync(string to, DateTime activatedAtUtc, string? deviceInfo, CancellationToken cancellationToken);
    }
}