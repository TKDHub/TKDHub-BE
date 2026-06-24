namespace Dojo.Domain.Enums;

/// <summary>
/// Manual checkout mode selected by the instructor. Not persisted on the invoice —
/// it only drives how many transactions are created at checkout and the initial
/// Open/Closed state. Actual paid/owed amounts are always derived from transactions.
/// </summary>
public enum PaymentStatusEnum : short
{
    Paid          = 1,
    PartiallyPaid = 2,
    NotPaid       = 3
}
