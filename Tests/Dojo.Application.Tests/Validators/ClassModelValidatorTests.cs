using Dojo.Application.Models.Class;
using Dojo.Application.Validators.Classes;
using FluentValidation.TestHelper;

namespace Dojo.Application.Tests.Validators;

public class ClassModelValidatorTests
{
    private readonly ClassModelValidator _sut = new();

    private static ClassModel Valid() => new()
    {
        Name = "Kids Beginners",
        StartTime = new TimeOnly(16, 0),
        EndTime = new TimeOnly(17, 0),
        Weekdays = [DayOfWeek.Monday, DayOfWeek.Wednesday]
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
    public void EndTime_NotAfterStartTime_HasError()
        => _sut.TestValidate(Valid() with { EndTime = new TimeOnly(16, 0) }).ShouldHaveValidationErrorFor(x => x.EndTime);

    [Fact]
    public void EndTime_BeforeStartTime_HasError()
        => _sut.TestValidate(Valid() with { EndTime = new TimeOnly(15, 0) }).ShouldHaveValidationErrorFor(x => x.EndTime);

    [Fact]
    public void Weekdays_Empty_HasError()
        => _sut.TestValidate(Valid() with { Weekdays = [] }).ShouldHaveValidationErrorFor(x => x.Weekdays);

    [Fact]
    public void Weekdays_ContainsInvalidValue_HasError()
        => _sut.TestValidate(Valid() with { Weekdays = [(DayOfWeek)99] }).ShouldHaveValidationErrorFor("Weekdays[0]");
}

public class AddStudentsToClassModelValidatorTests
{
    private readonly AddStudentsToClassModelValidator _sut = new();

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(new AddStudentsToClassModel { ClassId = Guid.NewGuid(), StudentIds = [Guid.NewGuid()] })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ClassId_Empty_HasError()
        => _sut.TestValidate(new AddStudentsToClassModel { ClassId = Guid.Empty, StudentIds = [Guid.NewGuid()] })
            .ShouldHaveValidationErrorFor(x => x.ClassId);

    [Fact]
    public void StudentIds_Empty_HasError()
        => _sut.TestValidate(new AddStudentsToClassModel { ClassId = Guid.NewGuid(), StudentIds = [] })
            .ShouldHaveValidationErrorFor(x => x.StudentIds);
}

public class RemoveStudentsFromClassModelValidatorTests
{
    private readonly RemoveStudentsFromClassModelValidator _sut = new();

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(new RemoveStudentsFromClassModel { ClassId = Guid.NewGuid(), StudentIds = [Guid.NewGuid()] })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void StudentIds_Empty_HasError()
        => _sut.TestValidate(new RemoveStudentsFromClassModel { ClassId = Guid.NewGuid(), StudentIds = [] })
            .ShouldHaveValidationErrorFor(x => x.StudentIds);
}

public class MoveStudentsToClassModelValidatorTests
{
    private readonly MoveStudentsToClassModelValidator _sut = new();

    private static MoveStudentsToClassModel Valid() => new()
    {
        FromClassId = Guid.NewGuid(),
        ToClassId = Guid.NewGuid(),
        StudentIds = [Guid.NewGuid()]
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void FromClassId_Empty_HasError()
        => _sut.TestValidate(Valid() with { FromClassId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.FromClassId);

    [Fact]
    public void ToClassId_Empty_HasError()
        => _sut.TestValidate(Valid() with { ToClassId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.ToClassId);

    [Fact]
    public void FromAndToClassId_Same_HasError()
    {
        var classId = Guid.NewGuid();
        _sut.TestValidate(Valid() with { FromClassId = classId, ToClassId = classId })
            .ShouldHaveValidationErrorFor(x => x.ToClassId);
    }

    [Fact]
    public void StudentIds_Empty_HasError()
        => _sut.TestValidate(Valid() with { StudentIds = [] }).ShouldHaveValidationErrorFor(x => x.StudentIds);
}
