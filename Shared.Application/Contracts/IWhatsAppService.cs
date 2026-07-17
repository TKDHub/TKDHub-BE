namespace Shared.Application.Contracts;

public interface IWhatsAppService
{
    Task SendMessageAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
