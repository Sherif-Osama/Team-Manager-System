using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace TeamManager.Infrastructure.Communication
{
    public sealed class EmailSender(IOptions<EmailOptions> emailOptions, IOptions<AppUrlOptions> appUrlOptions) : IEmailSender
    {
        private readonly EmailOptions _emailOptions = emailOptions.Value;
        private readonly AppUrlOptions _options = appUrlOptions.Value;
        private async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_emailOptions.FromName, _emailOptions.FromEmail));

            message.To.Add(MailboxAddress.Parse(to));

            message.Subject = subject;

            message.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

            using var smtpClient = new SmtpClient();

            var socketOptions = _emailOptions.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

            await smtpClient.ConnectAsync(_emailOptions.Host, _emailOptions.Port, socketOptions, cancellationToken);

            await smtpClient.AuthenticateAsync(_emailOptions.Username, _emailOptions.Password, cancellationToken);

            await smtpClient.SendAsync(message, cancellationToken);

            await smtpClient.DisconnectAsync(true, cancellationToken);
        }

        public async Task SendInvitationEmailAsync(string to, string invitationToken, CancellationToken cancellationToken)
        {
            var link = $"{_options.BaseUrl}/invitations/accept?token={Uri.EscapeDataString(invitationToken)}";

            var body = $""" <p>You have been invited to join a team.</p>  <p><a href="{link}">Accept Invitation</a></p> """;

            await SendAsync(to, "Team Invitation", body, cancellationToken);
        }

        public async Task SendEmailConfirmationAsync(string to, string confirmationToken, CancellationToken cancellationToken)
        {
            var link = $"{_options.BaseUrl}/account/confirm-email?token={Uri.EscapeDataString(confirmationToken)}";

            var body = $"""<p>Please confirm your new email address.</p> <p><a href="{link}">Confirm Email</a></p>""";

            await SendAsync(to, "Confirm Your Email", body, cancellationToken);
        }

        public async Task SendPasswordChangedNotificationAsync(string to, CancellationToken cancellationToken)
        {
            const string body = "<p>Your password was just changed. If this wasn't you, please contact support immediately.</p>";

            await SendAsync(to, "Your Password Was Changed", body, cancellationToken);
        }

        public async Task SendAccountDeletedAsync(string to, DateTime deletedAtUtc, string? deviceInfo, CancellationToken cancellationToken)
        {
            var subject = "Your account has been deleted";

            var body = $"""
                        <h2>Account Deleted</h2>
                        <p>Your TeamManager account has been deleted successfully.</p>
                        <p><strong>Deleted at:</strong> {deletedAtUtc:yyyy-MM-dd HH:mm:ss} UTC</p>
                        <p><strong>Device:</strong> {deviceInfo ?? "Unknown"}</p>
                    """;

            await SendAsync(to, subject, body, cancellationToken);
        }

        public async Task SendAccountDeactivatedAsync(string to, DateTime deactivatedAtUtc, string? deviceInfo, CancellationToken cancellationToken)
        {
            var subject = "Your account has been deactivated";
            var body = $"""
                <h2>Account Deactivated</h2>
                <p>Your TeamManager account has been deactivated successfully.</p>
                <p><strong>Deactivated at:</strong> {deactivatedAtUtc:yyyy-MM-dd HH:mm:ss} UTC</p>
                <p><strong>Device:</strong> {deviceInfo ?? "Unknown"}</p>

                <p>
                    Your account will remain deactivated for <strong>30 days</strong>.
                    If you do not log in during this period, your account will be
                    <strong>permanently deleted automatically</strong>.
                </p>

                <p>
                    To keep your account, simply log in again within 30 days.
                </p>
                """;

            await SendAsync(to, subject, body, cancellationToken);
        }

        public async Task SendAccountActivatedAsync(string to, DateTime activatedAtUtc, string? deviceInfo, CancellationToken cancellationToken)
        {
            var subject = "Your account has been activated";

            var body = $"""
                            <h2>Account Activated</h2>
                            <p>Your TeamManager account has been activated successfully.</p>
                            <p><strong>Activated at:</strong> {activatedAtUtc:yyyy-MM-dd HH:mm:ss} UTC</p>
                            <p><strong>Device:</strong> {deviceInfo ?? "Unknown"}</p>
                        """;

            await SendAsync(to, subject, body, cancellationToken);
        }
    }
}