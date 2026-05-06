using Identity.Application.Contracts;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
using Identity.Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Shared.Domain.Primitives;

namespace Identity.Infrastructure.Services
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

                // Use SslOnConnect for Port 465
                await client.ConnectAsync(
                    _smtp.Host,
                    _smtp.Port,
                    SecureSocketOptions.SslOnConnect,
                    cancellationToken);

                // Note: Authenticate MUST come after ConnectAsync
                await client.AuthenticateAsync(_smtp.Username, _smtp.Password, cancellationToken);
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(quit: true, cancellationToken);

                _logger.LogInformation("Email sent to {Email} — subject: {Subject}", toEmail, subject);

                return Result.Success(UserMessages.EmailSent);
            }
            catch (Exception ex)
            {
                return Result.Failure<string>(new("User.EmailSentErorr", ex.Message));
                throw;
            }
        }
    }
}
