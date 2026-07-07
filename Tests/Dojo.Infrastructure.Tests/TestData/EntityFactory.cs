using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
using Shared.Domain.Enums;

namespace Dojo.Infrastructure.Tests.TestData;

internal static class EntityFactory
{
    public static SubscriptionPlan NewPlan(Guid tenantId, Guid branchId, string name = "Basic") => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        BranchId = branchId,
        Name = name,
        DurationMonths = 3,
        Price = 100m,
        StatusId = (short)EntityStatusEnum.Active,
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "system@tkdhub.com",
        CreatedByName = "System"
    };

    public static Student NewStudent(Guid tenantId, Guid branchId, Guid planId, string firstName = "John") => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        BranchId = branchId,
        FirstName = firstName,
        LastName = "Doe",
        Email = $"{Guid.NewGuid()}@test.com",
        PhoneNumber = "0700000000",
        DateOfBirth = new DateOnly(2000, 1, 1),
        Gender = GenderEnum.Male,
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
        BeltLevel = BeltLevelEnum.White,
        SubscriptionPlanId = planId,
        Price = 100m,
        Currency = "JOD",
        DurationMonths = 3,
        StatusId = (short)EntityStatusEnum.Active,
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "system@tkdhub.com",
        CreatedByName = "System"
    };

    public static IncomeInvoice NewIncomeInvoice(
        Guid tenantId,
        Guid branchId,
        Guid studentId,
        decimal originalPrice = 100m,
        IncomeInvoiceStatusEnum status = IncomeInvoiceStatusEnum.Open,
        short entityStatus = (short)EntityStatusEnum.Active) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        BranchId = branchId,
        StudentId = studentId,
        Type = IncomeInvoiceTypeEnum.Subscription,
        OriginalPrice = originalPrice,
        DiscountValue = 0m,
        Currency = "JOD",
        Status = status,
        StatusId = entityStatus,
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "system@tkdhub.com",
        CreatedByName = "System"
    };

    public static IncomeTransaction NewTransaction(
        Guid branchId,
        Guid invoiceId,
        decimal amount,
        IncomeTransactionStatusEnum status = IncomeTransactionStatusEnum.Paid) => new()
    {
        Id = Guid.NewGuid(),
        BranchId = branchId,
        IncomeInvoiceId = invoiceId,
        Amount = amount,
        Method = PaymentMethodEnum.Cash,
        Status = status,
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "system@tkdhub.com",
        CreatedByName = "System"
    };

    public static OutcomeInvoice NewOutcomeInvoice(
        Guid tenantId,
        Guid branchId,
        decimal amount,
        string title = "Rent",
        short entityStatus = (short)EntityStatusEnum.Active) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        BranchId = branchId,
        Title = title,
        Amount = amount,
        Currency = "JOD",
        StatusId = entityStatus,
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "system@tkdhub.com",
        CreatedByName = "System"
    };
}
