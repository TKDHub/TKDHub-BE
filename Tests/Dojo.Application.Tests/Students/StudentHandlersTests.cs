using Dojo.Application.Commands.Students;
using Dojo.Application.Models.Student;
using Dojo.Application.Queries.Students;
using Dojo.Domain.Constants;
using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly IStudentActivityLogRepository _activityLogs = Substitute.For<IStudentActivityLogRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateStudentCommandHandler CreateSut() => new(_students, _plans, _branchService, _activityLogs, _uow, NullLogger<CreateStudentCommandHandler>.Instance);

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
        Assert.Equal(added.StartDate.AddMonths(plan.DurationMonths), added.EndDate);
        Assert.Equal(added.EndDate, result.Value.EndDate);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _activityLogs.Received(1).Add(Arg.Is<StudentActivityLog>(l =>
            l.StudentId == added.Id && l.ActivityType == StudentActivityType.Created));
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
    private readonly IStudentActivityLogRepository _activityLogs = Substitute.For<IStudentActivityLogRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private UpdateStudentCommandHandler CreateSut() => new(_students, _plans, _activityLogs, _uow, NullLogger<UpdateStudentCommandHandler>.Instance);

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
        _activityLogs.Received(1).Add(Arg.Is<StudentActivityLog>(l =>
            l.StudentId == student.Id && l.ActivityType == StudentActivityType.Updated));
    }

    [Fact]
    public async Task Handle_WhenStartDateChanges_RecalculatesEndDateFromExistingDuration()
    {
        var id = Guid.NewGuid();
        var plan = StudentTestData.MakePlan();
        var student = StudentTestData.Make(id); // DurationMonths = 1, frozen at registration
        _students.GetByIdIgnoringFiltersAsync(id, Arg.Any<CancellationToken>()).Returns(student);
        _plans.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        var newStartDate = new DateOnly(2026, 3, 1);

        var result = await CreateSut().Handle(
            new UpdateStudentCommand(StudentTestData.Model(plan.Id) with { StudentId = id, Email = "ali@acme.test", StartDate = newStartDate }), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(newStartDate.AddMonths(student.DurationMonths), student.EndDate);
        Assert.Equal(student.EndDate, result.Value.EndDate);
    }
}

public class DeleteStudentCommandHandlerTests
{
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly IStudentActivityLogRepository _activityLogs = Substitute.For<IStudentActivityLogRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private DeleteStudentCommandHandler CreateSut() => new(_students, _activityLogs, _uow, NullLogger<DeleteStudentCommandHandler>.Instance);

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _students.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Student?)null);

        var result = await CreateSut().Handle(new DeleteStudentCommand(Guid.NewGuid(), "a@a.test", "A"), default);

        Assert.Equal(StudentErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenFound_RemovesAndSaves()
    {
        var student = StudentTestData.Make();
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);

        var result = await CreateSut().Handle(new DeleteStudentCommand(student.Id, "a@a.test", "A"), default);

        Assert.True(result.IsSuccess);
        _students.Received(1).Remove(student);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _activityLogs.Received(1).Add(Arg.Is<StudentActivityLog>(l =>
            l.StudentId == student.Id && l.ActivityType == StudentActivityType.Deleted));
    }
}

public class FreezeStudentCommandHandlerTests
{
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly IStudentActivityLogRepository _activityLogs = Substitute.For<IStudentActivityLogRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private FreezeStudentCommandHandler CreateSut() => new(_students, _activityLogs, _uow, NullLogger<FreezeStudentCommandHandler>.Instance);

    private static FreezeStudentCommand Command(Guid studentId) =>
        new(new FreezeStudentModel { StudentId = studentId, FrozenByEmail = "a@a.test", FrozenByName = "A" });

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _students.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Student?)null);

        var result = await CreateSut().Handle(Command(Guid.NewGuid()), default);

        Assert.Equal(StudentErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenAlreadyFrozen_ReturnsAlreadyFrozen()
    {
        var student = StudentTestData.Make(status: (short)StudentStatusEnum.Frozen);
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);

        var result = await CreateSut().Handle(Command(student.Id), default);

        Assert.Equal(StudentErrors.AlreadyFrozen, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_SnapshotsRemainingDaysAndFreezes()
    {
        // Mirrors the worked example: StartDate 1/5, 3-month duration (EndDate 1/8), frozen
        // partway through — the days left until EndDate must be preserved, not discarded.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var student = StudentTestData.Make();
        student.EndDate = today.AddDays(20);
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);

        var result = await CreateSut().Handle(Command(student.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal((short)StudentStatusEnum.Frozen, student.StatusId);
        Assert.Equal(today, student.FrozenOn);
        Assert.Equal(20, student.RemainingDurationDays);
        Assert.Equal("a@a.test", student.FrozenByEmail);
        Assert.Equal("A", student.FrozenByName);
        Assert.Equal(20, result.Value.RemainingDurationDays);
        Assert.Equal("Frozen", result.Value.Status);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _activityLogs.Received(1).Add(Arg.Is<StudentActivityLog>(l =>
            l.StudentId == student.Id && l.ActivityType == StudentActivityType.Frozen));
    }

    [Fact]
    public async Task Handle_WhenEndDateAlreadyPassed_ClampsRemainingDaysToZero()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var student = StudentTestData.Make();
        student.EndDate = today.AddDays(-5); // membership already expired before the freeze
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);

        var result = await CreateSut().Handle(Command(student.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, student.RemainingDurationDays);
    }
}

public class ReactivateStudentCommandHandlerTests
{
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly ISubscriptionPlanRepository _plans = Substitute.For<ISubscriptionPlanRepository>();
    private readonly IStudentActivityLogRepository _activityLogs = Substitute.For<IStudentActivityLogRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ReactivateStudentCommandHandler CreateSut() => new(_students, _plans, _activityLogs, _uow, NullLogger<ReactivateStudentCommandHandler>.Instance);

    private static ReactivateStudentModel Model(Guid studentId, DateOnly startDate, Guid? planId = null) => new()
    {
        StudentId = studentId,
        StartDate = startDate,
        SubscriptionPlanId = planId,
        ModifiedByEmail = "a@a.test",
        ModifiedByName = "A"
    };

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _students.GetByIdIncludingDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Student?)null);

        var result = await CreateSut().Handle(new ReactivateStudentCommand(Model(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow))), default);

        Assert.Equal(StudentErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenAlreadyActive_ReturnsAlreadyActive()
    {
        var student = StudentTestData.Make(status: (short)StudentStatusEnum.Active);
        _students.GetByIdIncludingDeletedAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);

        var result = await CreateSut().Handle(new ReactivateStudentCommand(Model(student.Id, DateOnly.FromDateTime(DateTime.UtcNow))), default);

        Assert.Equal(StudentErrors.AlreadyActive, result.Error);
    }

    [Fact]
    public async Task Handle_WhenFrozen_ResumesFromRemainingDays()
    {
        // Worked example: frozen with 20 days left — reactivating on a new start date must
        // land the new EndDate exactly 20 days after it, not recompute from StartDate/duration.
        var student = StudentTestData.Make(status: (short)StudentStatusEnum.Frozen);
        student.RemainingDurationDays = 20;
        student.FrozenOn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10);
        student.FrozenByEmail = "old@old.test";
        student.FrozenByName = "Old";
        _students.GetByIdIncludingDeletedAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);

        var newStart = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await CreateSut().Handle(new ReactivateStudentCommand(Model(student.Id, newStart)), default);

        Assert.True(result.IsSuccess);
        Assert.Equal((short)StudentStatusEnum.Active, student.StatusId);
        Assert.Equal(newStart, student.StartDate);
        Assert.Equal(newStart.AddDays(20), student.EndDate);
        Assert.Null(student.FrozenOn);
        Assert.Null(student.FrozenByEmail);
        Assert.Null(student.FrozenByName);
        Assert.Null(student.RemainingDurationDays);
        await _plans.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _activityLogs.Received(1).Add(Arg.Is<StudentActivityLog>(l =>
            l.StudentId == student.Id && l.ActivityType == StudentActivityType.Reactivated));
    }

    [Fact]
    public async Task Handle_WhenInactive_ReRegistersAgainstItsOwnPlan_WhenNoneGiven()
    {
        var plan = StudentTestData.MakePlan();
        var student = StudentTestData.Make(status: (short)StudentStatusEnum.Inactive);
        student.SubscriptionPlanId = plan.Id;
        _students.GetByIdIncludingDeletedAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);
        _plans.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        var newStart = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await CreateSut().Handle(new ReactivateStudentCommand(Model(student.Id, newStart)), default);

        Assert.True(result.IsSuccess);
        Assert.Equal((short)StudentStatusEnum.Active, student.StatusId);
        Assert.Equal(newStart, student.StartDate);
        Assert.Equal(newStart.AddMonths(plan.DurationMonths), student.EndDate);
        Assert.Equal(plan.Price, student.Price);
        Assert.Equal(plan.DurationMonths, student.DurationMonths);
    }

    [Fact]
    public async Task Handle_WhenInactive_UsesGivenPlanInsteadOfExisting()
    {
        var newPlan = StudentTestData.MakePlan();
        newPlan.DurationMonths = 6;
        var student = StudentTestData.Make(status: (short)StudentStatusEnum.Inactive);
        _students.GetByIdIncludingDeletedAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);
        _plans.GetByIdAsync(newPlan.Id, Arg.Any<CancellationToken>()).Returns(newPlan);

        var newStart = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await CreateSut().Handle(new ReactivateStudentCommand(Model(student.Id, newStart, newPlan.Id)), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(newPlan.Id, student.SubscriptionPlanId);
        Assert.Equal(6, student.DurationMonths);
        Assert.Equal(newStart.AddMonths(6), student.EndDate);
    }

    [Fact]
    public async Task Handle_WhenInactiveAndPlanNotFound_ReturnsNoActivePlans()
    {
        var student = StudentTestData.Make(status: (short)StudentStatusEnum.Inactive);
        _students.GetByIdIncludingDeletedAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);
        _plans.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SubscriptionPlan?)null);

        var result = await CreateSut().Handle(new ReactivateStudentCommand(Model(student.Id, DateOnly.FromDateTime(DateTime.UtcNow))), default);

        Assert.Equal(StudentErrors.NoActivePlans, result.Error);
    }

    [Fact]
    public async Task Handle_WhenInactiveAndPlanNotActive_ReturnsSubscriptionNotActive()
    {
        var plan = StudentTestData.MakePlan(status: (short)EntityStatusEnum.Inactive);
        var student = StudentTestData.Make(status: (short)StudentStatusEnum.Inactive);
        student.SubscriptionPlanId = plan.Id;
        _students.GetByIdIncludingDeletedAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);
        _plans.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        var result = await CreateSut().Handle(new ReactivateStudentCommand(Model(student.Id, DateOnly.FromDateTime(DateTime.UtcNow))), default);

        Assert.Equal(StudentErrors.SubscriptionNotActive, result.Error);
    }
}

public class UploadStudentImageCommandHandlerTests
{
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly IImageService _images = Substitute.For<IImageService>();
    private readonly IStudentActivityLogRepository _activityLogs = Substitute.For<IStudentActivityLogRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private UploadStudentImageCommandHandler CreateSut() => new(_students, _images, _activityLogs, _uow, NullLogger<UploadStudentImageCommandHandler>.Instance);

    private static UploadStudentImageCommand Command(Guid studentId) =>
        new(studentId, Stream.Null, "photo.png", "image/png", 1024, "a@a.test", "A");

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
        _activityLogs.Received(1).Add(Arg.Is<StudentActivityLog>(l =>
            l.StudentId == student.Id && l.ActivityType == StudentActivityType.ImageUploaded));
    }
}

public class StudentQueryHandlersTests
{
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _students.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Student?)null);

        var result = await new GetStudentByIdQueryHandler(_students, NullLogger<GetStudentByIdQueryHandler>.Instance)
            .Handle(new GetStudentByIdQuery(Guid.NewGuid()), default);

        Assert.Equal(StudentErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsDto()
    {
        var student = StudentTestData.Make(firstName: "Ali", lastName: "Hassan");
        _students.GetByIdAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);

        var result = await new GetStudentByIdQueryHandler(_students, NullLogger<GetStudentByIdQueryHandler>.Instance)
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

        var result = await new GetAllStudentsQueryHandler(_students, userContext, branchContext, NullLogger<GetAllStudentsQueryHandler>.Instance)
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

        var result = await new GetAllStudentsQueryHandler(_students, userContext, branchContext, NullLogger<GetAllStudentsQueryHandler>.Instance)
            .Handle(new GetAllStudentsQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        await _students.Received(1).GetPagedAsync(Arg.Any<PagedRequest>(), branchId, Arg.Any<CancellationToken>());
    }
}

public class GetStudentActivityLogsQueryHandlerTests
{
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly IStudentActivityLogRepository _activityLogs = Substitute.For<IStudentActivityLogRepository>();

    private GetStudentActivityLogsQueryHandler CreateSut() => new(_students, _activityLogs, NullLogger<GetStudentActivityLogsQueryHandler>.Instance);

    [Fact]
    public async Task Handle_WhenStudentNotFound_ReturnsNotFound()
    {
        _students.GetByIdIncludingDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Student?)null);

        var result = await CreateSut().Handle(new GetStudentActivityLogsQuery(Guid.NewGuid(), new PagedRequest()), default);

        Assert.Equal(StudentErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenFound_ReturnsPagedLogs()
    {
        var student = StudentTestData.Make();
        _students.GetByIdIncludingDeletedAsync(student.Id, Arg.Any<CancellationToken>()).Returns(student);

        var log = new StudentActivityLog
        {
            StudentId = student.Id,
            TenantId = student.TenantId,
            BranchId = student.BranchId,
            ActivityType = StudentActivityType.Created,
            Description = "Student was registered.",
            CreatedOn = DateTimeOffset.UtcNow,
            CreatedByEmail = "a@a.test",
            CreatedByName = "A"
        };
        _activityLogs.GetPagedByStudentIdAsync(student.Id, Arg.Any<PagedRequest>(), Arg.Any<CancellationToken>())
            .Returns(PagedResult<StudentActivityLog>.Create([log], 1, 1, 20));

        var result = await CreateSut().Handle(new GetStudentActivityLogsQuery(student.Id, new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        var row = Assert.Single(result.Value.Items);
        Assert.Equal("Created", row.ActivityType);
        Assert.Equal(student.Id, row.StudentId);
    }
}
