using Dojo.Application.Commands.OutcomeInvoices;
using Dojo.Application.Models.OutcomeInvoice;
using Dojo.Application.Queries.OutcomeInvoices;
using Dojo.Domain.Constants;
using Dojo.Domain.Entities;
using Dojo.Domain.Repositories;
using NSubstitute;
using Shared.Application.Contracts;
using Shared.Application.Models;
using Shared.Domain.Enums;
using Shared.Domain.Pagination;
using Shared.Domain.Repositories;

namespace Dojo.Application.Tests.OutcomeInvoices;

internal static class OutcomeInvoiceTestData
{
    public static OutcomeInvoice Make(Guid? id = null, Guid? branchId = null, short status = 1) => new()
    {
        Id = id ?? Guid.NewGuid(),
        BranchId = branchId ?? Guid.NewGuid(),
        Title = "Rent",
        Amount = 200m,
        Currency = "JOD",
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "a@a.test",
        CreatedByName = "A",
        StatusId = status
    };

    public static CreateOutcomeInvoiceModel Model(string title = "Rent", decimal amount = 200m) =>
        new() { Title = title, Amount = amount };
}

public class CreateOutcomeInvoiceCommandHandlerTests
{
    private readonly IOutcomeInvoiceRepository _invoices = Substitute.For<IOutcomeInvoiceRepository>();
    private readonly IBranchService _branchService = Substitute.For<IBranchService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateOutcomeInvoiceCommandHandler CreateSut() => new(_invoices, _branchService, _uow);

    [Fact]
    public async Task Handle_WhenBranchIdEmpty_ReturnsBranchRequired()
    {
        var result = await CreateSut().Handle(
            new CreateOutcomeInvoiceCommand(OutcomeInvoiceTestData.Model(), Guid.Empty, Guid.NewGuid()), default);
        Assert.Equal(OutcomeInvoiceErrors.BranchRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenTitleMissing_ReturnsTitleRequired()
    {
        var result = await CreateSut().Handle(
            new CreateOutcomeInvoiceCommand(OutcomeInvoiceTestData.Model(title: " "), Guid.NewGuid(), Guid.NewGuid()), default);
        Assert.Equal(OutcomeInvoiceErrors.TitleRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenAmountNotPositive_ReturnsAmountInvalid()
    {
        var result = await CreateSut().Handle(
            new CreateOutcomeInvoiceCommand(OutcomeInvoiceTestData.Model(amount: 0), Guid.NewGuid(), Guid.NewGuid()), default);
        Assert.Equal(OutcomeInvoiceErrors.AmountInvalid, result.Error);
    }

    [Fact]
    public async Task Handle_WhenBranchNotFound_ReturnsBranchNotFound()
    {
        var branchId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>()).Returns((BranchInfo?)null);

        var result = await CreateSut().Handle(
            new CreateOutcomeInvoiceCommand(OutcomeInvoiceTestData.Model(), branchId, Guid.NewGuid()), default);

        Assert.Equal(OutcomeInvoiceErrors.BranchNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenTenantMismatch_ReturnsTenantBranchMismatch()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = Guid.NewGuid(), Currency = "JOD", Enabled = true });

        var result = await CreateSut().Handle(
            new CreateOutcomeInvoiceCommand(OutcomeInvoiceTestData.Model(), branchId, tenantId), default);

        Assert.Equal(OutcomeInvoiceErrors.TenantBranchMismatch, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_AddsWithBranchCurrencyAndSaves()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = tenantId, Currency = "JOD", Enabled = true });

        var result = await CreateSut().Handle(
            new CreateOutcomeInvoiceCommand(OutcomeInvoiceTestData.Model(), branchId, tenantId), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("JOD", result.Value.Currency);
        Assert.Null(result.Value.AttachmentUrl);
        _invoices.Received(1).Add(Arg.Any<OutcomeInvoice>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class DeleteOutcomeInvoiceCommandHandlerTests
{
    private readonly IOutcomeInvoiceRepository _invoices = Substitute.For<IOutcomeInvoiceRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private DeleteOutcomeInvoiceCommandHandler CreateSut() => new(_invoices, _uow);

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _invoices.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((OutcomeInvoice?)null);

        var result = await CreateSut().Handle(new DeleteOutcomeInvoiceCommand(Guid.NewGuid()), default);

        Assert.Equal(OutcomeInvoiceErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenFound_SoftDeletesAndSaves()
    {
        var invoice = OutcomeInvoiceTestData.Make();
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await CreateSut().Handle(new DeleteOutcomeInvoiceCommand(invoice.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal((short)EntityStatusEnum.Deleted, invoice.StatusId);
        _invoices.Received(1).Update(invoice);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class UploadOutcomeInvoiceAttachmentCommandHandlerTests
{
    private readonly IOutcomeInvoiceRepository _invoices = Substitute.For<IOutcomeInvoiceRepository>();
    private readonly IImageService _images = Substitute.For<IImageService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private UploadOutcomeInvoiceAttachmentCommandHandler CreateSut() => new(_invoices, _images, _uow);

    private static UploadOutcomeInvoiceAttachmentCommand Command(Guid invoiceId) =>
        new(invoiceId, Stream.Null, "receipt.png", "image/png", 1024);

    [Fact]
    public async Task Handle_WhenInvoiceNotFound_ReturnsNotFound()
    {
        _invoices.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((OutcomeInvoice?)null);

        var result = await CreateSut().Handle(Command(Guid.NewGuid()), default);

        Assert.Equal(OutcomeInvoiceErrors.NotFound, result.Error);
    }

    [Theory]
    [InlineData(FileValidationResult.Empty, "OutcomeInvoice.AttachmentEmpty")]
    [InlineData(FileValidationResult.TooLarge, "OutcomeInvoice.AttachmentTooLarge")]
    [InlineData(FileValidationResult.InvalidType, "OutcomeInvoice.AttachmentInvalidType")]
    public async Task Handle_WhenValidationFails_ReturnsMappedError(FileValidationResult validation, string expectedCode)
    {
        var invoice = OutcomeInvoiceTestData.Make();
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _images.ValidateFile(1024, "image/png", Arg.Any<long>(), Arg.Any<IReadOnlyCollection<string>>()).Returns(validation);

        var result = await CreateSut().Handle(Command(invoice.Id), default);

        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenUploadThrows_ReturnsAttachmentUploadFailed()
    {
        var invoice = OutcomeInvoiceTestData.Make();
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _images.ValidateFile(1024, "image/png", Arg.Any<long>(), Arg.Any<IReadOnlyCollection<string>>()).Returns(FileValidationResult.Valid);
        _images.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new InvalidOperationException("cdn down"));

        var result = await CreateSut().Handle(Command(invoice.Id), default);

        Assert.Equal(OutcomeInvoiceErrors.AttachmentUploadFailed, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_SetsAttachmentUrlAndSaves()
    {
        var invoice = OutcomeInvoiceTestData.Make();
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _images.ValidateFile(1024, "image/png", Arg.Any<long>(), Arg.Any<IReadOnlyCollection<string>>()).Returns(FileValidationResult.Valid);
        _images.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://cdn.test/receipt.png");

        var result = await CreateSut().Handle(Command(invoice.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://cdn.test/receipt.png", invoice.AttachmentUrl);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class OutcomeInvoiceQueryHandlersTests
{
    private readonly IOutcomeInvoiceRepository _invoices = Substitute.For<IOutcomeInvoiceRepository>();

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _invoices.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((OutcomeInvoice?)null);

        var result = await new GetOutcomeInvoiceByIdQueryHandler(_invoices)
            .Handle(new GetOutcomeInvoiceByIdQuery(Guid.NewGuid()), default);

        Assert.Equal(OutcomeInvoiceErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsDto()
    {
        var invoice = OutcomeInvoiceTestData.Make();
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await new GetOutcomeInvoiceByIdQueryHandler(_invoices)
            .Handle(new GetOutcomeInvoiceByIdQuery(invoice.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Rent", result.Value.Title);
    }

    [Fact]
    public async Task GetAll_WhenBranchAdmin_ScopesToBranch()
    {
        var branchId = Guid.NewGuid();
        var userContext = Substitute.For<IUserContext>();
        var branchContext = Substitute.For<IBranchContext>();
        userContext.IsSuperAdmin.Returns(false);
        branchContext.BranchId.Returns(branchId);
        var paged = PagedResult<OutcomeInvoice>.Create(new List<OutcomeInvoice> { OutcomeInvoiceTestData.Make() }, 1, 1, 20);
        _invoices.GetPagedAsync(Arg.Any<PagedRequest>(), branchId, Arg.Any<CancellationToken>()).Returns(paged);

        var result = await new GetAllOutcomeInvoicesQueryHandler(_invoices, userContext, branchContext)
            .Handle(new GetAllOutcomeInvoicesQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
    }
}
