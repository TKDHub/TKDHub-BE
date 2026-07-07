using Dojo.Application.Models.SubscriptionPlan;
using Dojo.Application.Validators.SubscriptionPlans;
using FluentValidation.TestHelper;

namespace Dojo.Application.Tests.Validators;

public class SubscriptionPlanModelValidatorTests
{
    private readonly SubscriptionPlanModelValidator _sut = new();

    private static SubscriptionPlanModel Valid() => new()
    {
        Name = "Basic Plan",
        DurationMonths = 3,
        Price = 100m
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Name_Empty_HasError()
        => _sut.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Name_TooLong_HasError()
        => _sut.TestValidate(Valid() with { Name = new string('a', 201) }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Description_TooLong_WhenProvided_HasError()
        => _sut.TestValidate(Valid() with { Description = new string('a', 1001) }).ShouldHaveValidationErrorFor(x => x.Description);

    [Fact]
    public void DurationMonths_Zero_HasError()
        => _sut.TestValidate(Valid() with { DurationMonths = 0 }).ShouldHaveValidationErrorFor(x => x.DurationMonths);

    [Fact]
    public void Price_Negative_HasError()
        => _sut.TestValidate(Valid() with { Price = -1 }).ShouldHaveValidationErrorFor(x => x.Price);
}
