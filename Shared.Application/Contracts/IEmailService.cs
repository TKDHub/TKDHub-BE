using Shared.Domain.Primitives;

namespace Shared.Application.Contracts
{
    public interface IEmailService
    {
        Task<Result<string>> SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default);
    }
}
