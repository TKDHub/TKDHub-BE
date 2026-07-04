namespace Dojo.Application.Dtos.OutcomeInvoices;

public sealed class OutcomeInvoiceDto
{
    public Guid    Id       { get; init; }
    public Guid    TenantId { get; init; }
    public Guid    BranchId { get; init; }

    public string  Title         { get; init; } = string.Empty;
    public decimal Amount        { get; init; }
    public string  Currency      { get; init; } = string.Empty;
    public string? AttachmentUrl { get; init; }
    public string? Note          { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset  CreatedOn  { get; init; }
    public DateTimeOffset? ModifiedOn { get; init; }
}
