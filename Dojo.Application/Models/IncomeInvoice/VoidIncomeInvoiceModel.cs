namespace Dojo.Application.Models.IncomeInvoice;

public sealed record VoidIncomeInvoiceModel
{
    public Guid   InvoiceId { get; set; }
    public string Reason    { get; init; } = string.Empty;

    // ── Set by the controller from JWT claims ────────────────────
    public string VoidedByEmail { get; set; } = string.Empty;
    public string VoidedByName  { get; set; } = string.Empty;
}
