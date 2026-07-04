namespace Dojo.Application.Models.IncomeInvoice;

public sealed record RefundIncomeTransactionModel
{
    public Guid    InvoiceId     { get; init; }
    public Guid    TransactionId { get; init; }
    public decimal Amount        { get; init; }
    public string  Reason        { get; init; } = string.Empty;

    // ── Set by the controller from JWT claims ────────────────────
    public string RefundedByEmail { get; set; } = string.Empty;
    public string RefundedByName  { get; set; } = string.Empty;
}
