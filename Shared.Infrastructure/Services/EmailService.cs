using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Shared.Application.Contracts;
using Shared.Domain.Primitives;
using Shared.Infrastructure.Settings;

namespace Shared.Infrastructure.Services
{
    internal sealed class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtp;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> smtp, ILogger<EmailService> logger)
        {
            _smtp = smtp.Value;
            _logger = logger;
        }

        public async Task<Result<string>> SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;
                message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

                using var client = new SmtpClient();

                // Port 587 uses STARTTLS; Port 465 uses SslOnConnect
                var socketOptions = _smtp.Port == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await client.ConnectAsync(
                    _smtp.Host,
                    _smtp.Port,
                    socketOptions,
                    cancellationToken);

                // Note: Authenticate MUST come after ConnectAsync
                await client.AuthenticateAsync(_smtp.Username, _smtp.Password, cancellationToken);
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(quit: true, cancellationToken);

                _logger.LogInformation("Email sent to {Email} — subject: {Subject}", toEmail, subject);

                return Result.Success("Email sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                return Result.Failure<string>(new Error("Email.SendFailed", ex.Message));
            }
        }
    }
}
