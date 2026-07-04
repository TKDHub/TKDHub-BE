using Dojo.Application.Dtos.IncomeInvoices;
using Dojo.Application.Dtos.OutcomeInvoices;
using Shared.Domain.Pagination;

namespace Dojo.Application.Dtos.InvoicesSummary;

public sealed class InvoicesSummaryDto
{
    /// <summary>Net collected across matching income invoices: sum of Paid minus sum of Refund transactions.</summary>
    public decimal TotalIncome    { get; init; }
    public string  IncomeCurrency { get; init; } = string.Empty;

    /// <summary>Sum of Amount across Active outcome invoices only.</summary>
    public decimal TotalOutcome    { get; init; }
    public string  OutcomeCurrency { get; init; } = string.Empty;

    public PagedResult<IncomeInvoiceDto>   IncomeInvoices  { get; init; } = null!;
    public PagedResult<OutcomeInvoiceDto>  OutcomeInvoices { get; init; } = null!;
}
