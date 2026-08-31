using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using TeamManager.Application.Abstractions.Communication;

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
    }
}