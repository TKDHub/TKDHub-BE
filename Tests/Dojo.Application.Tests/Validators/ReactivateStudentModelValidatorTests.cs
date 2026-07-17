using Dojo.Application.Models.Student;
using Dojo.Application.Validators.Students;
using FluentValidation.TestHelper;

namespace Dojo.Application.Tests.Validators;

public class ReactivateStudentModelValidatorTests
{
    private readonly ReactivateStudentModelValidator _sut = new();

    private static ReactivateStudentModel Valid() => new()
    {
        StudentId = Guid.NewGuid(),
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow)
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void StudentId_Empty_HasError()
        => _sut.TestValidate(Valid() with { StudentId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.StudentId);

    [Fact]
    public void StartDate_MinValue_HasError()
        => _sut.TestValidate(Valid() with { StartDate = DateOnly.MinValue }).ShouldHaveValidationErrorFor(x => x.StartDate);

    [Fact]
    public void SubscriptionPlanId_Null_IsValid()
        => _sut.TestValidate(Valid() with { SubscriptionPlanId = null }).ShouldNotHaveValidationErrorFor(x => x.SubscriptionPlanId);
}
