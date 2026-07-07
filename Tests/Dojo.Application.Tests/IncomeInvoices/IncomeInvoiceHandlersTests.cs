using Dojo.Application.Commands.IncomeInvoices;
using Dojo.Application.Models.IncomeInvoice;
using Dojo.Application.Queries.IncomeInvoices;
using Dojo.Domain.Constants;
using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using NSubstitute;
using Shared.Application.Contracts;
using Shared.Application.Models;
using Shared.Domain.Pagination;
using Shared.Domain.Repositories;

namespace Dojo.Application.Tests.IncomeInvoices;

internal static class InvoiceTestData
{
    public static Student MakeStudent(Guid? id = null, Guid? branchId = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        BranchId = branchId ?? Guid.NewGuid(),
        FirstName = "Ali",
        LastName = "Hassan",
        PhoneNumber = "+1000",
        SubscriptionPlanId = Guid.NewGuid(),
        Price = 50m,
        Currency = "JOD",
        DurationMonths = 1,
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "a@a.test",
        CreatedByName = "A",
        StatusId = 1
    };

    public static IncomeInvoice MakeInvoice(Guid? id = null, Guid? branchId = null, decimal originalPrice = 100m,
        IncomeInvoiceStatusEnum status = IncomeInvoiceStatusEnum.Open) => new()
    {
        Id = id ?? Guid.NewGuid(),
        BranchId = branchId ?? Guid.NewGuid(),
        StudentId = Guid.NewGuid(),
        Type = IncomeInvoiceTypeEnum.Subscription,
        OriginalPrice = originalPrice,
        Currency = "JOD",
        Status = status,
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "a@a.test",
        CreatedByName = "A",
        StatusId = 1
    };

    public static IncomeTransaction MakePaidTransaction(IncomeInvoice invoice, decimal amount) => new()
    {
        Id = Guid.NewGuid(),
        BranchId = invoice.BranchId,
        IncomeInvoiceId = invoice.Id,
        Amount = amount,
        Method = PaymentMethodEnum.Cash,
        Status = IncomeTransactionStatusEnum.Paid,
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "a@a.test",
        CreatedByName = "A"
    };

    public static CreateIncomeInvoiceModel CreateModel(Guid? studentId = null, decimal originalPrice = 100m,
        decimal? amountPaid = null, PaymentMethodEnum? method = null) => new()
    {
        StudentId = studentId ?? Guid.NewGuid(),
        Type = IncomeInvoiceTypeEnum.Subscription,
        OriginalPrice = originalPrice,
        AmountPaid = amountPaid,
        PaymentMethod = method
    };
}

public class CreateIncomeInvoiceCommandHandlerTests
{
    private readonly IIncomeInvoiceRepository _invoices = Substitute.For<IIncomeInvoiceRepository>();
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly IBranchService _branchService = Substitute.For<IBranchService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateIncomeInvoiceCommandHandler CreateSut() => new(_invoices, _students, _branchService, _uow);

    [Fact]
    public async Task Handle_WhenStudentIdEmpty_ReturnsStudentRequired()
    {
        var result = await CreateSut().Handle(new CreateIncomeInvoiceCommand(InvoiceTestData.CreateModel(Guid.Empty)), default);
        Assert.Equal(IncomeInvoiceErrors.StudentRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenPriceNotPositive_ReturnsPriceInvalid()
    {
        var result = await CreateSut().Handle(new CreateIncomeInvoiceCommand(InvoiceTestData.CreateModel(originalPrice: 0)), default);
        Assert.Equal(IncomeInvoiceErrors.PriceInvalid, result.Error);
    }

    [Fact]
    public async Task Handle_WhenPercentageDiscountOutOfRange_ReturnsDiscountInvalid()
    {
        var model = InvoiceTestData.CreateModel() with { DiscountType = DiscountTypeEnum.Percentage, DiscountValue = 150 };
        var result = await CreateSut().Handle(new CreateIncomeInvoiceCommand(model), default);
        Assert.Equal(IncomeInvoiceErrors.DiscountInvalid, result.Error);
    }

    [Fact]
    public async Task Handle_WhenDiscountValueNegative_ReturnsDiscountInvalid()
    {
        var model = InvoiceTestData.CreateModel() with { DiscountValue = -1 };
        var result = await CreateSut().Handle(new CreateIncomeInvoiceCommand(model), default);
        Assert.Equal(IncomeInvoiceErrors.DiscountInvalid, result.Error);
    }

    [Fact]
    public async Task Handle_WhenStudentNotFound_ReturnsStudentNotFound()
    {
        var studentId = Guid.NewGuid();
        _students.GetByIdAsync(studentId, Arg.Any<CancellationToken>()).Returns((Student?)null);

        var result = await CreateSut().Handle(new CreateIncomeInvoiceCommand(InvoiceTestData.CreateModel(studentId)), default);

        Assert.Equal(IncomeInvoiceErrors.StudentNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenBranchNotFound_ReturnsBranchNotFound()
    {
        var student = InvoiceTestData.MakeStudent();
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);
        _branchService.GetBranchAsync(student.BranchId, Arg.Any<CancellationToken>()).Returns((BranchInfo?)null);

        var result = await CreateSut().Handle(new CreateIncomeInvoiceCommand(InvoiceTestData.CreateModel(student.Id)), default);

        Assert.Equal(IncomeInvoiceErrors.BranchNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenAmountPaidExceedsTotal_ReturnsPaymentExceedsTotal()
    {
        var student = InvoiceTestData.MakeStudent();
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);
        _branchService.GetBranchAsync(student.BranchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = student.BranchId, TenantId = Guid.NewGuid(), Currency = "JOD", Enabled = true });

        var model = InvoiceTestData.CreateModel(student.Id, originalPrice: 100, amountPaid: 150, method: PaymentMethodEnum.Cash);
        var result = await CreateSut().Handle(new CreateIncomeInvoiceCommand(model), default);

        Assert.Equal(IncomeInvoiceErrors.PaymentExceedsTotal, result.Error);
    }

    [Fact]
    public async Task Handle_WhenAmountPaidButNoPaymentMethod_ReturnsPaymentMethodRequired()
    {
        var student = InvoiceTestData.MakeStudent();
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);
        _branchService.GetBranchAsync(student.BranchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = student.BranchId, TenantId = Guid.NewGuid(), Currency = "JOD", Enabled = true });

        var model = InvoiceTestData.CreateModel(student.Id, originalPrice: 100, amountPaid: 50, method: null);
        var result = await CreateSut().Handle(new CreateIncomeInvoiceCommand(model), default);

        Assert.Equal(IncomeInvoiceErrors.PaymentMethodRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenNoAmountPaid_CreatesOpenInvoiceWithNoTransactions()
    {
        var student = InvoiceTestData.MakeStudent();
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);
        _branchService.GetBranchAsync(student.BranchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = student.BranchId, TenantId = Guid.NewGuid(), Currency = "JOD", Enabled = true });

        IncomeInvoice? added = null;
        _invoices.When(r => r.Add(Arg.Any<IncomeInvoice>())).Do(c => added = c.Arg<IncomeInvoice>());

        var model = InvoiceTestData.CreateModel(student.Id, originalPrice: 100, amountPaid: null);
        var result = await CreateSut().Handle(new CreateIncomeInvoiceCommand(model), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(IncomeInvoiceStatusEnum.Open, added!.Status);
        Assert.Empty(added.Transactions);
        Assert.Equal("JOD", added.Currency);
    }

    [Fact]
    public async Task Handle_WhenFullyPaid_CreatesClosedInvoiceWithOneTransaction()
    {
        var student = InvoiceTestData.MakeStudent();
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);
        _branchService.GetBranchAsync(student.BranchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = student.BranchId, TenantId = Guid.NewGuid(), Currency = "JOD", Enabled = true });

        IncomeInvoice? added = null;
        _invoices.When(r => r.Add(Arg.Any<IncomeInvoice>())).Do(c => added = c.Arg<IncomeInvoice>());

        var model = InvoiceTestData.CreateModel(student.Id, originalPrice: 100, amountPaid: 100, method: PaymentMethodEnum.Cash);
        var result = await CreateSut().Handle(new CreateIncomeInvoiceCommand(model), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(IncomeInvoiceStatusEnum.Closed, added!.Status);
        Assert.Single(added.Transactions);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPartiallyPaid_CreatesOpenInvoiceWithOneTransaction()
    {
        var student = InvoiceTestData.MakeStudent();
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);
        _branchService.GetBranchAsync(student.BranchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = student.BranchId, TenantId = Guid.NewGuid(), Currency = "JOD", Enabled = true });

        IncomeInvoice? added = null;
        _invoices.When(r => r.Add(Arg.Any<IncomeInvoice>())).Do(c => added = c.Arg<IncomeInvoice>());

        var model = InvoiceTestData.CreateModel(student.Id, originalPrice: 100, amountPaid: 40, method: PaymentMethodEnum.Cash);
        var result = await CreateSut().Handle(new CreateIncomeInvoiceCommand(model), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(IncomeInvoiceStatusEnum.Open, added!.Status);
        Assert.Equal(40, result.Value.AmountPaid);
        Assert.Equal(60, result.Value.RemainingAmount);
    }
}

public class AddIncomeTransactionCommandHandlerTests
{
    private readonly IIncomeInvoiceRepository _invoices = Substitute.For<IIncomeInvoiceRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private AddIncomeTransactionCommandHandler CreateSut() => new(_invoices, _uow);

    private static AddIncomeTransactionModel Model(Guid invoiceId, decimal amount) =>
        new() { IncomeInvoiceId = invoiceId, Amount = amount, Method = PaymentMethodEnum.Cash };

    [Fact]
    public async Task Handle_WhenInvoiceNotFound_ReturnsNotFound()
    {
        _invoices.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((IncomeInvoice?)null);

        var result = await CreateSut().Handle(new AddIncomeTransactionCommand(Model(Guid.NewGuid(), 10)), default);

        Assert.Equal(IncomeInvoiceErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenInvoiceVoided_ReturnsInvoiceVoided()
    {
        var invoice = InvoiceTestData.MakeInvoice(status: IncomeInvoiceStatusEnum.Voided);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new AddIncomeTransactionCommand(Model(invoice.Id, 10)), default);

        Assert.Equal(IncomeInvoiceErrors.InvoiceVoided, result.Error);
    }

    [Fact]
    public async Task Handle_WhenInvoiceClosed_ReturnsAlreadyClosed()
    {
        var invoice = InvoiceTestData.MakeInvoice(status: IncomeInvoiceStatusEnum.Closed);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new AddIncomeTransactionCommand(Model(invoice.Id, 10)), default);

        Assert.Equal(IncomeInvoiceErrors.AlreadyClosed, result.Error);
    }

    [Fact]
    public async Task Handle_WhenAmountExceedsRemaining_ReturnsTransactionAmountInvalid()
    {
        var invoice = InvoiceTestData.MakeInvoice(originalPrice: 100);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new AddIncomeTransactionCommand(Model(invoice.Id, 150)), default);

        Assert.Equal(IncomeInvoiceErrors.TransactionAmountInvalid, result.Error);
    }

    [Fact]
    public async Task Handle_WhenAmountZeroOrNegative_ReturnsTransactionAmountInvalid()
    {
        var invoice = InvoiceTestData.MakeInvoice(originalPrice: 100);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new AddIncomeTransactionCommand(Model(invoice.Id, 0)), default);

        Assert.Equal(IncomeInvoiceErrors.TransactionAmountInvalid, result.Error);
    }

    [Fact]
    public async Task Handle_WhenPartialPayment_KeepsInvoiceOpen()
    {
        var invoice = InvoiceTestData.MakeInvoice(originalPrice: 100);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new AddIncomeTransactionCommand(Model(invoice.Id, 40)), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(IncomeInvoiceStatusEnum.Open, invoice.Status);
        Assert.Single(invoice.Transactions);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPaymentCoversFullBalance_ClosesInvoice()
    {
        var invoice = InvoiceTestData.MakeInvoice(originalPrice: 100);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new AddIncomeTransactionCommand(Model(invoice.Id, 100)), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(IncomeInvoiceStatusEnum.Closed, invoice.Status);
    }
}

public class VoidIncomeInvoiceCommandHandlerTests
{
    private readonly IIncomeInvoiceRepository _invoices = Substitute.For<IIncomeInvoiceRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private VoidIncomeInvoiceCommandHandler CreateSut() => new(_invoices, _uow);

    private static VoidIncomeInvoiceModel Model(Guid invoiceId) =>
        new() { InvoiceId = invoiceId, Reason = "requested by student", VoidedByEmail = "a@a.test", VoidedByName = "A" };

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _invoices.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((IncomeInvoice?)null);

        var result = await CreateSut().Handle(new VoidIncomeInvoiceCommand(Model(Guid.NewGuid())), default);

        Assert.Equal(IncomeInvoiceErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenAlreadyVoided_ReturnsAlreadyVoided()
    {
        var invoice = InvoiceTestData.MakeInvoice(status: IncomeInvoiceStatusEnum.Voided);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new VoidIncomeInvoiceCommand(Model(invoice.Id)), default);

        Assert.Equal(IncomeInvoiceErrors.AlreadyVoided, result.Error);
    }

    [Fact]
    public async Task Handle_WhenNoTransactions_JustMarksVoided()
    {
        var invoice = InvoiceTestData.MakeInvoice();
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new VoidIncomeInvoiceCommand(Model(invoice.Id)), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(IncomeInvoiceStatusEnum.Voided, invoice.Status);
        Assert.Empty(invoice.Transactions);
        Assert.Equal("requested by student", invoice.VoidReason);
    }

    [Fact]
    public async Task Handle_WhenPaidTransactionExists_CascadesFullRefund()
    {
        var invoice = InvoiceTestData.MakeInvoice(originalPrice: 100);
        var paid = InvoiceTestData.MakePaidTransaction(invoice, 100);
        invoice.Transactions.Add(paid);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new VoidIncomeInvoiceCommand(Model(invoice.Id)), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(IncomeInvoiceStatusEnum.Voided, invoice.Status);
        Assert.Equal(2, invoice.Transactions.Count);
        var refund = invoice.Transactions.Single(t => t.Status == IncomeTransactionStatusEnum.Refund);
        Assert.Equal(100, refund.Amount);
        Assert.Equal(paid.Id, refund.RefundOfTransactionId);
        Assert.Equal(0, invoice.RemainingAmount); // forced to zero once voided
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPartiallyAlreadyRefunded_OnlyRefundsRemainingBalance()
    {
        var invoice = InvoiceTestData.MakeInvoice(originalPrice: 100);
        var paid = InvoiceTestData.MakePaidTransaction(invoice, 100);
        invoice.Transactions.Add(paid);
        invoice.Transactions.Add(new IncomeTransaction
        {
            Id = Guid.NewGuid(),
            BranchId = invoice.BranchId,
            IncomeInvoiceId = invoice.Id,
            Amount = 30,
            Method = PaymentMethodEnum.Cash,
            Status = IncomeTransactionStatusEnum.Refund,
            RefundOfTransactionId = paid.Id,
            CreatedOn = DateTimeOffset.UtcNow,
            CreatedByEmail = "a@a.test",
            CreatedByName = "A"
        });
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new VoidIncomeInvoiceCommand(Model(invoice.Id)), default);

        Assert.True(result.IsSuccess);
        var newRefund = invoice.Transactions.Single(t => t.Status == IncomeTransactionStatusEnum.Refund && t.Amount == 70);
        Assert.Equal(paid.Id, newRefund.RefundOfTransactionId);
    }
}

public class RefundIncomeTransactionCommandHandlerTests
{
    private readonly IIncomeInvoiceRepository _invoices = Substitute.For<IIncomeInvoiceRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private RefundIncomeTransactionCommandHandler CreateSut() => new(_invoices, _uow);

    private static RefundIncomeTransactionModel Model(Guid invoiceId, Guid txnId, decimal amount) => new()
    {
        InvoiceId = invoiceId,
        TransactionId = txnId,
        Amount = amount,
        Reason = "partial refund",
        RefundedByEmail = "a@a.test",
        RefundedByName = "A"
    };

    [Fact]
    public async Task Handle_WhenInvoiceNotFound_ReturnsNotFound()
    {
        _invoices.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((IncomeInvoice?)null);

        var result = await CreateSut().Handle(new RefundIncomeTransactionCommand(Model(Guid.NewGuid(), Guid.NewGuid(), 10)), default);

        Assert.Equal(IncomeInvoiceErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenInvoiceVoided_ReturnsCannotRefundVoidedInvoice()
    {
        var invoice = InvoiceTestData.MakeInvoice(status: IncomeInvoiceStatusEnum.Voided);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new RefundIncomeTransactionCommand(Model(invoice.Id, Guid.NewGuid(), 10)), default);

        Assert.Equal(IncomeInvoiceErrors.CannotRefundVoidedInvoice, result.Error);
    }

    [Fact]
    public async Task Handle_WhenTransactionNotFound_ReturnsTransactionNotFound()
    {
        var invoice = InvoiceTestData.MakeInvoice();
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new RefundIncomeTransactionCommand(Model(invoice.Id, Guid.NewGuid(), 10)), default);

        Assert.Equal(IncomeInvoiceErrors.TransactionNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenTransactionNotPaid_ReturnsTransactionNotPaid()
    {
        var invoice = InvoiceTestData.MakeInvoice(originalPrice: 100);
        var paid = InvoiceTestData.MakePaidTransaction(invoice, 100);
        invoice.Transactions.Add(paid);
        var existingRefund = paid.ToTestRefund(50);
        invoice.Transactions.Add(existingRefund);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        // attempt to refund the refund transaction itself
        var result = await CreateSut().Handle(new RefundIncomeTransactionCommand(Model(invoice.Id, existingRefund.Id, 10)), default);

        Assert.Equal(IncomeInvoiceErrors.TransactionNotPaid, result.Error);
    }

    [Fact]
    public async Task Handle_WhenAmountExceedsRefundable_ReturnsRefundAmountInvalid()
    {
        var invoice = InvoiceTestData.MakeInvoice(originalPrice: 100);
        var paid = InvoiceTestData.MakePaidTransaction(invoice, 100);
        invoice.Transactions.Add(paid);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new RefundIncomeTransactionCommand(Model(invoice.Id, paid.Id, 150)), default);

        Assert.Equal(IncomeInvoiceErrors.RefundAmountInvalid, result.Error);
    }

    [Fact]
    public async Task Handle_WhenPartialRefund_ReopensClosedInvoice()
    {
        var invoice = InvoiceTestData.MakeInvoice(originalPrice: 100, status: IncomeInvoiceStatusEnum.Closed);
        var paid = InvoiceTestData.MakePaidTransaction(invoice, 100);
        invoice.Transactions.Add(paid);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new RefundIncomeTransactionCommand(Model(invoice.Id, paid.Id, 30)), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(IncomeInvoiceStatusEnum.Open, invoice.Status);
        Assert.Equal(70, invoice.AmountPaid);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFullRefundOfOnlyTransaction_AutoVoidsInvoice()
    {
        var invoice = InvoiceTestData.MakeInvoice(originalPrice: 100, status: IncomeInvoiceStatusEnum.Closed);
        var paid = InvoiceTestData.MakePaidTransaction(invoice, 100);
        invoice.Transactions.Add(paid);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new RefundIncomeTransactionCommand(Model(invoice.Id, paid.Id, 100)), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(IncomeInvoiceStatusEnum.Voided, invoice.Status);
        Assert.Contains("Auto-voided", invoice.VoidReason);
    }

    [Fact]
    public async Task Handle_WhenMultipleTransactionsAndOnlyOneFullyRefunded_DoesNotAutoVoid()
    {
        var invoice = InvoiceTestData.MakeInvoice(originalPrice: 150);
        var paid1 = InvoiceTestData.MakePaidTransaction(invoice, 100);
        var paid2 = InvoiceTestData.MakePaidTransaction(invoice, 50);
        invoice.Transactions.Add(paid1);
        invoice.Transactions.Add(paid2);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new RefundIncomeTransactionCommand(Model(invoice.Id, paid1.Id, 100)), default);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(IncomeInvoiceStatusEnum.Voided, invoice.Status);
    }
}

internal static class RefundTestHelper
{
    public static IncomeTransaction ToTestRefund(this IncomeTransaction original, decimal amount) => new()
    {
        Id = Guid.NewGuid(),
        BranchId = original.BranchId,
        IncomeInvoiceId = original.IncomeInvoiceId,
        Amount = amount,
        Method = original.Method,
        Status = IncomeTransactionStatusEnum.Refund,
        RefundOfTransactionId = original.Id,
        RefundedOn = DateTimeOffset.UtcNow,
        RefundedByEmail = "a@a.test",
        RefundedByName = "A",
        RefundReason = "test",
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "a@a.test",
        CreatedByName = "A"
    };
}

public class IncomeInvoiceQueryHandlersTests
{
    private readonly IIncomeInvoiceRepository _invoices = Substitute.For<IIncomeInvoiceRepository>();

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _invoices.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((IncomeInvoice?)null);

        var result = await new GetIncomeInvoiceByIdQueryHandler(_invoices)
            .Handle(new GetIncomeInvoiceByIdQuery(Guid.NewGuid()), default);

        Assert.Equal(IncomeInvoiceErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsDto()
    {
        var invoice = InvoiceTestData.MakeInvoice();
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await new GetIncomeInvoiceByIdQueryHandler(_invoices)
            .Handle(new GetIncomeInvoiceByIdQuery(invoice.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(invoice.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetAll_WhenSuperAdmin_QueriesAcrossAllBranches()
    {
        var userContext = Substitute.For<IUserContext>();
        var branchContext = Substitute.For<IBranchContext>();
        userContext.IsSuperAdmin.Returns(true);
        var paged = PagedResult<IncomeInvoice>.Create(new List<IncomeInvoice> { InvoiceTestData.MakeInvoice() }, 1, 1, 20);
        _invoices.GetPagedAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>()).Returns(paged);

        var result = await new GetAllIncomeInvoicesQueryHandler(_invoices, userContext, branchContext)
            .Handle(new GetAllIncomeInvoicesQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        await _invoices.Received(1).GetPagedAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>());
    }
}
