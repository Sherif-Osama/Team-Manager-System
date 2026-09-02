namespace TeamManager.Application.Common.Outbox
{
    public enum OutboxMessageType
    {
        InvitationEmail = 1,
        EmailConfirmationEmail = 2,
        PasswordChangedNotificationEmail = 3
    }
}