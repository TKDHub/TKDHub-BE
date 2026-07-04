namespace Dojo.Application.Models.OutcomeInvoice;

/// <summary>
/// Bound from a multipart/form-data request. The attachment file itself is handled
/// separately by the controller (Stream/FileName/ContentType are not Application-layer
/// concerns) and passed alongside this model into the command.
/// </summary>
public sealed record CreateOutcomeInvoiceModel
{
    public string  Title  { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Note   { get; init; }

    // ── Set by the controller from JWT claims ────────────────────
    public string CreatedByEmail { get; set; } = string.Empty;
    public string CreatedByName  { get; set; } = string.Empty;
}
