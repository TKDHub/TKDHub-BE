using Dojo.Application.Models.OutcomeInvoice;
using Dojo.Application.Validators.OutcomeInvoices;
using FluentValidation.TestHelper;

namespace Dojo.Application.Tests.Validators;

public class CreateOutcomeInvoiceModelValidatorTests
{
    private readonly CreateOutcomeInvoiceModelValidator _sut = new();

    private static CreateOutcomeInvoiceModel Valid() => new()
    {
        Title = "Rent",
        Amount = 500m
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Title_Empty_HasError()
        => _sut.TestValidate(Valid() with { Title = "" }).ShouldHaveValidationErrorFor(x => x.Title);

    [Fact]
    public void Title_TooLong_HasError()
        => _sut.TestValidate(Valid() with { Title = new string('a', 201) }).ShouldHaveValidationErrorFor(x => x.Title);

    [Fact]
    public void Amount_Zero_HasError()
        => _sut.TestValidate(Valid() with { Amount = 0 }).ShouldHaveValidationErrorFor(x => x.Amount);

    [Fact]
    public void Note_TooLong_WhenProvided_HasError()
        => _sut.TestValidate(Valid() with { Note = new string('a', 1001) }).ShouldHaveValidationErrorFor(x => x.Note);

    [Fact]
    public void Note_Null_IsIgnored()
        => _sut.TestValidate(Valid() with { Note = null }).ShouldNotHaveValidationErrorFor(x => x.Note);
}
