namespace Dojo.Application.Dtos.IncomeInvoices;

public sealed class IncomeTransactionDto
{
    public Guid           Id              { get; init; }
    public Guid           IncomeInvoiceId { get; init; }
    public decimal        Amount          { get; init; }
    public string         Method          { get; init; } = string.Empty;
    public DateTimeOffset CreatedOn       { get; init; }
}
