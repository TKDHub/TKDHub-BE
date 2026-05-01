namespace Identity.Application.Contracts
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default);
    }
}
