using Dojo.Domain.Enums;

namespace Dojo.Application.Models.IncomeInvoice;

public sealed record AddIncomeTransactionModel
{
    public Guid              IncomeInvoiceId { get; set; }
    public decimal           Amount          { get; init; }
    public PaymentMethodEnum Method          { get; init; }

    // ── Set by the controller from JWT claims ────────────────────
    public string CreatedByEmail { get; set; } = string.Empty;
    public string CreatedByName  { get; set; } = string.Empty;
}
