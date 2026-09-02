namespace TeamManager.Infrastructure.Communication
{
    public interface IEmailSender
    {
        Task SendInvitationEmailAsync(string to, string invitationToken, CancellationToken cancellationToken);
        Task SendEmailConfirmationAsync(string to, string confirmationToken, CancellationToken cancellationToken);
        Task SendPasswordChangedNotificationAsync(string to, CancellationToken cancellationToken);
    }
}