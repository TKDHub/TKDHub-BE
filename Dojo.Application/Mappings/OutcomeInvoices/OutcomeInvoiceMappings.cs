using Dojo.Application.Dtos.OutcomeInvoices;
using Dojo.Application.Models.OutcomeInvoice;
using Dojo.Domain.Entities;
using Shared.Domain.Enums;

namespace Dojo.Application.Mappings.OutcomeInvoices;

public static class OutcomeInvoiceMappings
{
    public static OutcomeInvoice ToEntity(
        this CreateOutcomeInvoiceModel model,
        Guid    branchId,
        Guid    tenantId,
        string  currency,
        string? attachmentUrl)
        => new()
        {
            BranchId       = branchId,
            TenantId       = tenantId,
            Title          = model.Title.Trim(),
            Amount         = model.Amount,
            Currency       = currency,
            AttachmentUrl  = attachmentUrl,
            Note           = model.Note?.Trim(),
            CreatedOn      = DateTimeOffset.UtcNow,
            CreatedByEmail = model.CreatedByEmail,
            CreatedByName  = model.CreatedByName
        };

    public static List<OutcomeInvoiceDto> ToListDtos(this IEnumerable<OutcomeInvoice> invoices)
        => invoices.Select(i => i.ToDto()).ToList();

    public static OutcomeInvoiceDto ToDto(this OutcomeInvoice invoice)
        => new()
        {
            Id       = invoice.Id,
            TenantId = invoice.TenantId,
            BranchId = invoice.BranchId,

            Title         = invoice.Title,
            Amount        = invoice.Amount,
            Currency      = invoice.Currency,
            AttachmentUrl = invoice.AttachmentUrl,
            Note          = invoice.Note,

            Status = ((EntityStatusEnum)invoice.StatusId).ToString(),

            CreatedOn  = invoice.CreatedOn,
            ModifiedOn = invoice.ModifiedOn
        };
}
