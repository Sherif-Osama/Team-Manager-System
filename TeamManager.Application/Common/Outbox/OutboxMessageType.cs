namespace TeamManager.Application.Common.Outbox
{
    public enum OutboxMessageType
    {
        InvitationEmail = 1,
        EmailConfirmationEmail = 2,
        PasswordChangedNotificationEmail = 3,
        AccountDeactivatedEmail = 4,
        AccountDeletedEmail = 5,
        AccountActivationEmail = 6,
    }
}