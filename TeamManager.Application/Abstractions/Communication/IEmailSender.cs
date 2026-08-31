namespace TeamManager.Application.Abstractions.Communication
{
    public interface IEmailSender
    {
        Task SendInvitationEmailAsync(string to, string invitationToken, CancellationToken cancellationToken);
    }
}