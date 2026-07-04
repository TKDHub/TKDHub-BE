namespace Dojo.Application.Dtos.IncomeInvoices;

public sealed class IncomeTransactionDto
{
    public Guid           Id              { get; init; }
    public Guid           IncomeInvoiceId { get; init; }
    public decimal        Amount          { get; init; }
    public string         Method          { get; init; } = string.Empty;

    /// <summary>Paid or Refund.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Set only on a Refund transaction — the original Paid transaction it refunds.</summary>
    public Guid? RefundOfTransactionId { get; init; }

    public DateTimeOffset? RefundedOn      { get; init; }
    public string?         RefundedByEmail { get; init; }
    public string?         RefundedByName  { get; init; }
    public string?         RefundReason    { get; init; }

    public DateTimeOffset CreatedOn { get; init; }
}
