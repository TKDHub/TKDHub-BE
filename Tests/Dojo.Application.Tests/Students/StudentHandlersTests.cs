using Dojo.Application.Commands.Students;
using Dojo.Application.Models.Student;
using Dojo.Application.Queries.Students;
using Dojo.Domain.Constants;
using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using NSubstitute;
using Shared.Application.Contracts;
using Shared.Application.Models;
using Shared.Domain.Enums;
using Shared.Domain.Pagination;
using Shared.Domain.Repositories;

namespace Dojo.Application.Tests.Students;

internal static class StudentTestData
{
    public static Student Make(Guid? id = null, Guid? branchId = null, string firstName = "Ali", string lastName = "Hassan",
        short status = 1) => new()
    {
        Id = id ?? Guid.NewGuid(),
        BranchId = branchId ?? Guid.NewGuid(),
        FirstName = firstName,
        LastName = lastName,
        Email = "ali@acme.test",
        PhoneNumber = "+1000",
        Gender = GenderEnum.Male,
        BeltLevel = BeltLevelEnum.White,
        SubscriptionPlanId = Guid.NewGuid(),
        Price = 50m,
        Currency = "JOD",
        DurationMonths = 1,
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "a@a.test",
        CreatedByName = "A",
        StatusId = status
    };

    public static SubscriptionPlan MakePlan(Guid? id = null, short status = 1) => new()
    {
        Id = id ?? Guid.NewGuid(),
        BranchId = Guid.NewGuid(),
        Name = "Monthly",
        DurationMonths = 1,
        Price = 50m,
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "a@a.test",
        CreatedByName = "A",
        StatusId = status
    };

    public static StudentModel Model(Guid? planId = null) => new()
    {
        FirstName = "Ali",
        LastName = "Hassan",
        Email = "ali@acme.test",
        PhoneNumber = "+1000",
        Gender = "Male",
        BeltLevel = "White",
        SubscriptionPlanId = planId ?? Guid.NewGuid()
    };
}

public class CreateStudentCommandHandlerTests
{
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly ISubscriptionPlanRepository _plans = Substitute.For<ISubscriptionPlanRepository>();
    private readonly IBranchService _branchService = Substitute.For<IBranchService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateStudentCommandHandler CreateSut() => new(_students, _plans, _branchService, _uow);

    [Fact]
    public async Task Handle_WhenBranchIdEmpty_ReturnsBranchRequired()
    {
        var result = await CreateSut().Handle(new CreateStudentCommand(StudentTestData.Model(), Guid.Empty, Guid.NewGuid()), default);
        Assert.Equal(StudentErrors.BranchRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenBranchNotFound_ReturnsBranchNotFound()
    {
        var branchId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>()).Returns((BranchInfo?)null);

        var result = await CreateSut().Handle(new CreateStudentCommand(StudentTestData.Model(), branchId, Guid.NewGuid()), default);

        Assert.Equal(StudentErrors.BranchNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenBranchTenantMismatch_ReturnsTenantBranchMismatch()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = Guid.NewGuid(), Currency = "JOD", Enabled = true });

        var result = await CreateSut().Handle(new CreateStudentCommand(StudentTestData.Model(), branchId, tenantId), default);

        Assert.Equal(StudentErrors.TenantBranchMismatch, result.Error);
    }

    [Fact]
    public async Task Handle_WhenFirstNameMissing_ReturnsFirstNameRequired()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = tenantId, Currency = "JOD", Enabled = true });

        var result = await CreateSut().Handle(
            new CreateStudentCommand(StudentTestData.Model() with { FirstName = " " }, branchId, tenantId), default);

        Assert.Equal(StudentErrors.FirstNameRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ReturnsEmailAlreadyExists()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = tenantId, Currency = "JOD", Enabled = true });
        _students.ExistsByEmailAsync("ali@acme.test", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(new CreateStudentCommand(StudentTestData.Model(), branchId, tenantId), default);

        Assert.Equal(StudentErrors.EmailAlreadyExists, result.Error);
    }

    [Fact]
    public async Task Handle_WhenSubscriptionPlanIdEmpty_ReturnsSubscriptionRequired()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = tenantId, Currency = "JOD", Enabled = true });
        _students.ExistsByEmailAsync("ali@acme.test", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut().Handle(
            new CreateStudentCommand(StudentTestData.Model(Guid.Empty), branchId, tenantId), default);

        Assert.Equal(StudentErrors.SubscriptionRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenPlanNotFound_ReturnsNoActivePlans()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = tenantId, Currency = "JOD", Enabled = true });
        _students.ExistsByEmailAsync("ali@acme.test", null, Arg.Any<CancellationToken>()).Returns(false);
        _plans.GetByIdAsync(planId, Arg.Any<CancellationToken>()).Returns((SubscriptionPlan?)null);

        var result = await CreateSut().Handle(new CreateStudentCommand(StudentTestData.Model(planId), branchId, tenantId), default);

        Assert.Equal(StudentErrors.NoActivePlans, result.Error);
    }

    [Fact]
    public async Task Handle_WhenPlanInactive_ReturnsSubscriptionNotActive()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var plan = StudentTestData.MakePlan(status: (short)EntityStatusEnum.Inactive);
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = tenantId, Currency = "JOD", Enabled = true });
        _students.ExistsByEmailAsync("ali@acme.test", null, Arg.Any<CancellationToken>()).Returns(false);
        _plans.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        var result = await CreateSut().Handle(new CreateStudentCommand(StudentTestData.Model(plan.Id), branchId, tenantId), default);

        Assert.Equal(StudentErrors.SubscriptionNotActive, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_SnapshotsPlanTermsAndBranchCurrency()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var plan = StudentTestData.MakePlan();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = tenantId, Currency = "JOD", Enabled = true });
        _students.ExistsByEmailAsync("ali@acme.test", null, Arg.Any<CancellationToken>()).Returns(false);
        _plans.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        Student? added = null;
        _students.When(r => r.Add(Arg.Any<Student>())).Do(c => added = c.Arg<Student>());

        var result = await CreateSut().Handle(new CreateStudentCommand(StudentTestData.Model(plan.Id), branchId, tenantId), default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal(plan.Price, added!.Price);
        Assert.Equal(plan.DurationMonths, added.DurationMonths);
        Assert.Equal("JOD", added.Currency);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBranchCurrencyMissing_DefaultsToNA()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var plan = StudentTestData.MakePlan();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = tenantId, Currency = null, Enabled = true });
        _students.ExistsByEmailAsync("ali@acme.test", null, Arg.Any<CancellationToken>()).Returns(false);
        _plans.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        Student? added = null;
        _students.When(r => r.Add(Arg.Any<Student>())).Do(c => added = c.Arg<Student>());

        await CreateSut().Handle(new CreateStudentCommand(StudentTestData.Model(plan.Id), branchId, tenantId), default);

        Assert.Equal("N/A", added!.Currency);
    }
}

public class UpdateStudentCommandHandlerTests
{
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly ISubscriptionPlanRepository _plans = Substitute.For<ISubscriptionPlanRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private UpdateStudentCommandHandler CreateSut() => new(_students, _plans, _uow);

    [Fact]
    public async Task Handle_WhenLastNameMissing_ReturnsLastNameRequired()
    {
        var result = await CreateSut().Handle(
            new UpdateStudentCommand(StudentTestData.Model() with { StudentId = Guid.NewGuid(), LastName = "" }), default);

        Assert.Equal(StudentErrors.LastNameRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenStudentNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _students.GetByIdIgnoringFiltersAsync(id, Arg.Any<CancellationToken>()).Returns((Student?)null);

        var result = await CreateSut().Handle(new UpdateStudentCommand(StudentTestData.Model() with { StudentId = id }), default);

        Assert.Equal(StudentErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenNewEmailTaken_ReturnsEmailAlreadyExists()
    {
        var id = Guid.NewGuid();
        var student = StudentTestData.Make(id);
        _students.GetByIdIgnoringFiltersAsync(id, Arg.Any<CancellationToken>()).Returns(student);
        _students.ExistsByEmailAsync("new@acme.test", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(
            new UpdateStudentCommand(StudentTestData.Model() with { StudentId = id, Email = "new@acme.test" }), default);

        Assert.Equal(StudentErrors.EmailAlreadyExists, result.Error);
    }

    [Fact]
    public async Task Handle_WhenPlanInactive_ReturnsSubscriptionNotActive()
    {
        var id = Guid.NewGuid();
        var plan = StudentTestData.MakePlan(status: (short)EntityStatusEnum.Inactive);
        _students.GetByIdIgnoringFiltersAsync(id, Arg.Any<CancellationToken>()).Returns(StudentTestData.Make(id));
        _plans.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        var result = await CreateSut().Handle(
            new UpdateStudentCommand(StudentTestData.Model(plan.Id) with { StudentId = id, Email = "ali@acme.test" }), default);

        Assert.Equal(StudentErrors.SubscriptionNotActive, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesAndSaves()
    {
        var id = Guid.NewGuid();
        var plan = StudentTestData.MakePlan();
        var student = StudentTestData.Make(id);
        _students.GetByIdIgnoringFiltersAsync(id, Arg.Any<CancellationToken>()).Returns(student);
        _plans.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        var result = await CreateSut().Handle(
            new UpdateStudentCommand(StudentTestData.Model(plan.Id) with { StudentId = id, FirstName = "Updated", Email = "ali@acme.test" }), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated", student.FirstName);
        _students.Received(1).Update(student);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class DeleteStudentCommandHandlerTests
{
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private DeleteStudentCommandHandler CreateSut() => new(_students, _uow);

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _students.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Student?)null);

        var result = await CreateSut().Handle(new DeleteStudentCommand(Guid.NewGuid()), default);

        Assert.Equal(StudentErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenFound_RemovesAndSaves()
    {
        var student = StudentTestData.Make();
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);

        var result = await CreateSut().Handle(new DeleteStudentCommand(student.Id), default);

        Assert.True(result.IsSuccess);
        _students.Received(1).Remove(student);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class UploadStudentImageCommandHandlerTests
{
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly IImageService _images = Substitute.For<IImageService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private UploadStudentImageCommandHandler CreateSut() => new(_students, _images, _uow);

    private static UploadStudentImageCommand Command(Guid studentId) =>
        new(studentId, Stream.Null, "photo.png", "image/png", 1024);

    [Fact]
    public async Task Handle_WhenStudentNotFound_ReturnsNotFound()
    {
        _students.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Student?)null);

        var result = await CreateSut().Handle(Command(Guid.NewGuid()), default);

        Assert.Equal(StudentErrors.NotFound, result.Error);
    }

    [Theory]
    [InlineData(FileValidationResult.Empty, "Student.ImageEmpty")]
    [InlineData(FileValidationResult.TooLarge, "Student.ImageTooLarge")]
    [InlineData(FileValidationResult.InvalidType, "Student.ImageInvalidType")]
    public async Task Handle_WhenValidationFails_ReturnsMappedError(FileValidationResult validation, string expectedCode)
    {
        var student = StudentTestData.Make();
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);
        _images.ValidateFile(1024, "image/png", Arg.Any<long>(), Arg.Any<IReadOnlyCollection<string>>()).Returns(validation);

        var result = await CreateSut().Handle(Command(student.Id), default);

        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenUploadThrows_ReturnsImageUploadFailed()
    {
        var student = StudentTestData.Make();
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);
        _images.ValidateFile(1024, "image/png", Arg.Any<long>(), Arg.Any<IReadOnlyCollection<string>>()).Returns(FileValidationResult.Valid);
        _images.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new InvalidOperationException("cdn down"));

        var result = await CreateSut().Handle(Command(student.Id), default);

        Assert.Equal(StudentErrors.ImageUploadFailed, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_SetsProfileImageUrlAndSaves()
    {
        var student = StudentTestData.Make();
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);
        _images.ValidateFile(1024, "image/png", Arg.Any<long>(), Arg.Any<IReadOnlyCollection<string>>()).Returns(FileValidationResult.Valid);
        _images.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://cdn.test/img.png");

        var result = await CreateSut().Handle(Command(student.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://cdn.test/img.png", result.Value);
        Assert.Equal("https://cdn.test/img.png", student.ProfileImageUrl);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class StudentQueryHandlersTests
{
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _students.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Student?)null);

        var result = await new GetStudentByIdQueryHandler(_students)
            .Handle(new GetStudentByIdQuery(Guid.NewGuid()), default);

        Assert.Equal(StudentErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsDto()
    {
        var student = StudentTestData.Make(firstName: "Ali", lastName: "Hassan");
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);

        var result = await new GetStudentByIdQueryHandler(_students)
            .Handle(new GetStudentByIdQuery(student.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Ali Hassan", result.Value.FullName);
    }

    [Fact]
    public async Task GetAll_WhenSuperAdmin_QueriesAcrossAllBranches()
    {
        var userContext = Substitute.For<IUserContext>();
        var branchContext = Substitute.For<IBranchContext>();
        userContext.IsSuperAdmin.Returns(true);
        var paged = PagedResult<Student>.Create(new List<Student> { StudentTestData.Make() }, 1, 1, 20);
        _students.GetPagedAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>()).Returns(paged);

        var result = await new GetAllStudentsQueryHandler(_students, userContext, branchContext)
            .Handle(new GetAllStudentsQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        await _students.Received(1).GetPagedAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAll_WhenBranchAdmin_ScopesToCurrentBranch()
    {
        var branchId = Guid.NewGuid();
        var userContext = Substitute.For<IUserContext>();
        var branchContext = Substitute.For<IBranchContext>();
        userContext.IsSuperAdmin.Returns(false);
        branchContext.BranchId.Returns(branchId);
        var paged = PagedResult<Student>.Empty(1, 20);
        _students.GetPagedAsync(Arg.Any<PagedRequest>(), branchId, Arg.Any<CancellationToken>()).Returns(paged);

        var result = await new GetAllStudentsQueryHandler(_students, userContext, branchContext)
            .Handle(new GetAllStudentsQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        await _students.Received(1).GetPagedAsync(Arg.Any<PagedRequest>(), branchId, Arg.Any<CancellationToken>());
    }
}
