using Dojo.Application.Models.Student;
using Dojo.Application.Validators.Students;
using FluentValidation.TestHelper;

namespace Dojo.Application.Tests.Validators;

public class StudentModelValidatorTests
{
    private readonly StudentModelValidator _sut = new();

    private static StudentModel Valid() => new()
    {
        FirstName = "John",
        LastName = "Doe",
        PhoneNumber = "0700000000",
        DateOfBirth = new DateOnly(2000, 1, 1),
        Gender = "Male",
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
        BeltLevel = "White",
        SubscriptionPlanId = Guid.NewGuid(),
        Price = 100m,
        Currency = "JOD",
        DurationMonths = 3
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void FirstName_Empty_HasError()
        => _sut.TestValidate(Valid() with { FirstName = "" }).ShouldHaveValidationErrorFor(x => x.FirstName);

    [Fact]
    public void LastName_TooLong_HasError()
        => _sut.TestValidate(Valid() with { LastName = new string('a', 151) }).ShouldHaveValidationErrorFor(x => x.LastName);

    [Fact]
    public void PhoneNumber_Empty_HasError()
        => _sut.TestValidate(Valid() with { PhoneNumber = "" }).ShouldHaveValidationErrorFor(x => x.PhoneNumber);

    [Fact]
    public void Email_Invalid_WhenProvided_HasError()
        => _sut.TestValidate(Valid() with { Email = "not-an-email" }).ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void Email_Null_IsIgnored()
        => _sut.TestValidate(Valid() with { Email = null }).ShouldNotHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void DateOfBirth_InTheFuture_HasError()
        => _sut.TestValidate(Valid() with { DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) })
            .ShouldHaveValidationErrorFor(x => x.DateOfBirth);

    [Fact]
    public void DateOfBirth_MoreThan120YearsAgo_HasError()
        => _sut.TestValidate(Valid() with { DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-121)) })
            .ShouldHaveValidationErrorFor(x => x.DateOfBirth);

    [Fact]
    public void Gender_Empty_HasError()
        => _sut.TestValidate(Valid() with { Gender = "" }).ShouldHaveValidationErrorFor(x => x.Gender);

    [Fact]
    public void StartDate_MinValue_HasError()
        => _sut.TestValidate(Valid() with { StartDate = DateOnly.MinValue }).ShouldHaveValidationErrorFor(x => x.StartDate);

    [Fact]
    public void BeltLevel_Empty_HasError()
        => _sut.TestValidate(Valid() with { BeltLevel = "" }).ShouldHaveValidationErrorFor(x => x.BeltLevel);

    [Fact]
    public void SubscriptionPlanId_Empty_HasError()
        => _sut.TestValidate(Valid() with { SubscriptionPlanId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.SubscriptionPlanId);

    [Fact]
    public void Price_Negative_HasError()
        => _sut.TestValidate(Valid() with { Price = -1 }).ShouldHaveValidationErrorFor(x => x.Price);

    [Fact]
    public void Currency_Empty_HasError()
        => _sut.TestValidate(Valid() with { Currency = "" }).ShouldHaveValidationErrorFor(x => x.Currency);

    [Fact]
    public void DurationMonths_Zero_HasError()
        => _sut.TestValidate(Valid() with { DurationMonths = 0 }).ShouldHaveValidationErrorFor(x => x.DurationMonths);

    [Fact]
    public void EmergencyContact_TooLong_WhenProvided_HasError()
        => _sut.TestValidate(Valid() with { EmergencyContact = new string('a', 201) })
            .ShouldHaveValidationErrorFor(x => x.EmergencyContact);
}
